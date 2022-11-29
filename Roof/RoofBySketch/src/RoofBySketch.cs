using System;
using System.Linq;
using Elements;
using Elements.Geometry;
using Elements.Geometry.Solids;
using GeometryEx;
using System.Collections.Generic;
using Vertex = Elements.Geometry.Vertex;

namespace RoofBySketch
{
    public static class RoofBySketch
    {
        /// <summary>
        /// Creates a Roof from a supplied Polygon sketch and a supplied elevation.
        /// </summary>
        /// <param name="model">The input model.</param>
        /// <param name="input">The arguments to the execution.</param>
        /// <returns>A RoofBySketchOutputs instance containing computed results and the model with any new elements.</returns>
        public static RoofBySketchOutputs Execute(Dictionary<string, Model> inputModels, RoofBySketchInputs input)
        {
            var topSide = new Elements.Geometry.Mesh();
            var area = 0.0;
            foreach (var triangle in input.Mesh.Triangles)
            {
                var a = topSide.AddVertex(triangle.Vertices[0].Position);
                var b = topSide.AddVertex(triangle.Vertices[1].Position);
                var c = topSide.AddVertex(triangle.Vertices[2].Position);
                var triAng =
                    new Elements.Geometry.Triangle(a, b, c);
                topSide.AddTriangle(triAng);
                area += triAng.Area();
            }
            topSide.ComputeNormals();

            // Find the Mesh's lowest point and use the
            // roof thickness to the set the Roof's underside elevation.
            var vertices = input.Mesh.Vertices.ToList();
            vertices = vertices.OrderBy(v => v.Position.Z).ToList();
            var elevation = vertices.First().Position.Z - input.Thickness;

            // Find the topSide Mesh's perimeter points and use them to
            // construct a Mesh representing the underside of the Roof.
            var perimeter = topSide.EdgesPerimeters().First();
            var ePoints = new List<Vector3>();
            perimeter.ForEach(e => ePoints.AddRange(e.Points()));
            ePoints = ePoints.Distinct().ToList();
            var uPoints = new List<Vector3>();
            ePoints.ForEach(p => uPoints.Add(new Vector3(p.X, p.Y, elevation)));
            var underBoundary = new Polygon(uPoints);
            var underSide = underBoundary.ToMesh(false);

            // Use the topSide Mesh's edgePoints and the lower Mesh's underPoints
            // to construct a series of triangles forming the sides of the Roof.
            var sideTriangles = new List<Elements.Geometry.Triangle>();
            for (var i = 0; i < ePoints.Count; i++)
            {
                sideTriangles.Add(
                    new Elements.Geometry.Triangle(new Vertex(ePoints[i]),
                                                   new Vertex(uPoints[i]),
                                                   new Vertex(uPoints[(i + 1) % uPoints.Count])));
                sideTriangles.Add(
                    new Elements.Geometry.Triangle(new Vertex(ePoints[i]),
                                                   new Vertex(uPoints[(i + 1) % uPoints.Count]),
                                                   new Vertex(ePoints[(i + 1) % ePoints.Count])));
            }

            // Construct the roof envelope in Elements.Geometry.mesh form.
            // We add vertices individually by position so that we don't affect
            // the original vertices of hte individual faces
            var Envelope = new Elements.Geometry.Mesh();

            foreach (var t in topSide.Triangles)
            {
                var a = Envelope.AddVertex(t.Vertices[0].Position);
                var b = Envelope.AddVertex(t.Vertices[1].Position);
                var c = Envelope.AddVertex(t.Vertices[2].Position);

                Envelope.AddTriangle(new Triangle(a, b, c));
            }
            foreach (var t in underSide.Triangles)
            {
                var a = Envelope.AddVertex(t.Vertices[0].Position);
                var b = Envelope.AddVertex(t.Vertices[1].Position);
                var c = Envelope.AddVertex(t.Vertices[2].Position);

                Envelope.AddTriangle(new Triangle(a, b, c));
            }
            foreach (var t in sideTriangles)
            {
                var a = Envelope.AddVertex(t.Vertices[0].Position);
                var b = Envelope.AddVertex(t.Vertices[1].Position);
                var c = Envelope.AddVertex(t.Vertices[2].Position);

                Envelope.AddTriangle(new Triangle(a, b, c));

            }
            // enVertices.ToList().ForEach(v => Envelope.AddVertex(v));
            // envTriangles.ToList().ForEach(t => Envelope.AddTriangle(t));
            Envelope.ComputeNormals();

            //Record roof high point from topSide mesh.
            var highPoint = topSide.Points().OrderByDescending(p => p.Z).First().Z;

            // // code for when debugging the function.
            // var envelope = MakeEnvelope();
            // var topside = MakeTopside();
            // var underside = MakeUnderside();
            // var underBoundary = Polygon.Rectangle(20.0, 20.0);
            // var elevation = 10.0;
            // var highPoint = 15.0;
            // var area = 100.0;

            var roof =
                new Roof(
                    Envelope,
                    topSide,
                    underSide,
                    underBoundary,
                    elevation,
                    highPoint,
                    input.Thickness,
                    area,
                    new Transform(),
                    BuiltInMaterials.Concrete,
                    null, false, Guid.NewGuid(), "Roof");
            var output = new RoofBySketchOutputs(area);


            output.Model.AddElement(new MeshElement(Envelope, material: BuiltInMaterials.Concrete));
            output.Model.AddElement(roof);
            return output;
        }

        static Mesh MakeDebugEnvelope()
        {
            var triangles = new List<Elements.Geometry.Triangle>
            {
                new Elements.Geometry.Triangle(new Vertex(new Vector3(2.0, 2.0, 12.0)),
                             new Vertex(new Vector3(9.0, 2.0, 12.0)),
                             new Vertex(new Vector3(5.0, 4.0, 10.0))),
                new Elements.Geometry.Triangle(new Vertex(new Vector3(5.0, 4.0, 10.0)),
                             new Vertex(new Vector3(9.0, 2.0, 12.0)),
                             new Vertex(new Vector3(5.0, 11.0, 10.0))),
                new Elements.Geometry.Triangle(new Vertex(new Vector3(5.0, 11.0, 10.0)),
                             new Vertex(new Vector3(9.0, 2.0, 12.0)),
                             new Vertex(new Vector3(9.0, 13.0, 12.0))),
                new Elements.Geometry.Triangle(new Vertex(new Vector3(9.0, 13.0, 12.0)),
                             new Vertex(new Vector3(2.0, 13.0, 12.0)),
                             new Vertex(new Vector3(5.0, 11.0, 10.0))),
                new Elements.Geometry.Triangle(new Vertex(new Vector3(2.0, 13.0, 12.0)),
                             new Vertex(new Vector3(2.0, 2.0, 12.0)),
                             new Vertex(new Vector3(5.0, 4.0, 10.0))),
                new Elements.Geometry.Triangle(new Vertex(new Vector3(5.0, 4.0, 10.0)),
                             new Vertex(new Vector3(5.0, 11.0, 10.0)),
                             new Vertex(new Vector3(2.0, 13.0, 12.0)))
            };
            var mesh = new Mesh();
            triangles.ForEach(t => mesh.AddTriangle(t));
            return mesh;
        }

        static Mesh MakeDebugTopside()
        {
            var triangles = new List<Elements.Geometry.Triangle>
            {
                new Elements.Geometry.Triangle(new Vertex(new Vector3(5.0, 4.0, 10.0)),
                             new Vertex(new Vector3(9.0, 2.0, 12.0)),
                             new Vertex(new Vector3(5.0, 11.0, 10.0))),
                new Elements.Geometry.Triangle(new Vertex(new Vector3(5.0, 11.0, 10.0)),
                             new Vertex(new Vector3(9.0, 2.0, 12.0)),
                             new Vertex(new Vector3(9.0, 13.0, 12.0))),
                new Elements.Geometry.Triangle(new Vertex(new Vector3(9.0, 13.0, 12.0)),
                             new Vertex(new Vector3(2.0, 13.0, 12.0)),
                             new Vertex(new Vector3(5.0, 11.0, 10.0))),
                new Elements.Geometry.Triangle(new Vertex(new Vector3(2.0, 13.0, 12.0)),
                             new Vertex(new Vector3(2.0, 2.0, 12.0)),
                             new Vertex(new Vector3(5.0, 4.0, 10.0))),
                new Elements.Geometry.Triangle(new Vertex(new Vector3(5.0, 4.0, 10.0)),
                             new Vertex(new Vector3(5.0, 11.0, 10.0)),
                             new Vertex(new Vector3(2.0, 13.0, 12.0)))
            };
            var mesh = new Mesh();
            triangles.ForEach(t => mesh.AddTriangle(t));
            return mesh;
        }

        static Mesh MakeDebugUnderside()
        {
            var triangles = new List<Elements.Geometry.Triangle>
            {
                new Elements.Geometry.Triangle(new Vertex(new Vector3(5.0, 11.0, 10.0)),
                             new Vertex(new Vector3(9.0, 2.0, 12.0)),
                             new Vertex(new Vector3(9.0, 13.0, 12.0))),
                new Elements.Geometry.Triangle(new Vertex(new Vector3(9.0, 13.0, 12.0)),
                             new Vertex(new Vector3(2.0, 13.0, 12.0)),
                             new Vertex(new Vector3(5.0, 11.0, 10.0))),
                new Elements.Geometry.Triangle(new Vertex(new Vector3(2.0, 13.0, 12.0)),
                             new Vertex(new Vector3(2.0, 2.0, 12.0)),
                             new Vertex(new Vector3(5.0, 4.0, 10.0))),
                new Elements.Geometry.Triangle(new Vertex(new Vector3(5.0, 4.0, 10.0)),
                             new Vertex(new Vector3(5.0, 11.0, 10.0)),
                             new Vertex(new Vector3(2.0, 13.0, 12.0)))
            };
            var mesh = new Mesh();
            triangles.ForEach(t => mesh.AddTriangle(t));
            return mesh;
        }
    }
}