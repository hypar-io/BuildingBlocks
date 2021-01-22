using System;
using System.Linq;
using Elements;
using Elements.Geometry;
using Elements.Geometry.Solids;
using GeometryEx;
using System.Collections.Generic;

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
            //var vertices = input.Mesh.Vertices.ToList();
            //var topSide = new Elements.Geometry.Mesh();
            //vertices.ForEach(v => topSide.AddVertex(v.Position));
            //var area = 0.0;
            //foreach (var triangle in input.Mesh.Triangles)
            //{
            //    var triAng =
            //        new Elements.Geometry.Triangle(triangle.Vertices[0], triangle.Vertices[1], triangle.Vertices[2]);
            //    topSide.AddTriangle(triAng);
            //    area += triAng.Area();
            //}
            //topSide.ComputeNormals();

            //// Find the Mesh's lowest point and use the 
            //// roof thickness to the set the Roof's underside elevation.
            //vertices = vertices.OrderBy(v => v.Position.Z).ToList();
            //var elevation = vertices.First().Position.Z - input.Thickness;

            //// Find the topSide Mesh's perimeter points and use them to
            //// construct a Mesh representing the underside of the Roof.
            //var perimeter = topSide.EdgesPerimeters().First();
            //var ePoints = new List<Vector3>();
            //perimeter.ForEach(e => ePoints.AddRange(e.Points()));
            //ePoints = ePoints.Distinct().ToList();
            //var uPoints = new List<Vector3>();
            //ePoints.ForEach(p => uPoints.Add(new Vector3(p.X, p.Y, elevation)));
            //var underBoundary = new Polygon(uPoints);
            //var underSide = underBoundary.ToMesh(false);

            //// Use the topSide Mesh's edgePoints and the lower Mesh's underPoints
            //// to construct a series of triangles forming the sides of the Roof.
            //var sideTriangles = new List<Elements.Geometry.Triangle>();
            //for(var i = 0; i < ePoints.Count; i++)
            //{
            //    sideTriangles.Add(
            //        new Elements.Geometry.Triangle(new Vertex(ePoints[i]),
            //                                       new Vertex(uPoints[i]),
            //                                       new Vertex(uPoints[(i + 1) % uPoints.Count])));
            //    sideTriangles.Add(
            //        new Elements.Geometry.Triangle(new Vertex(ePoints[i]),
            //                                       new Vertex(uPoints[(i + 1) % uPoints.Count]),
            //                                       new Vertex(ePoints[(i + 1) % ePoints.Count])));
            //}

            //// Create an aggregated list of Triangles representing the Roof envelope.
            //var envTriangles = new List<Elements.Geometry.Triangle>();
            //topSide.Triangles.ToList().ForEach(t => envTriangles.Add(t));
            //underSide.Triangles.ToList().ForEach(t => envTriangles.Add(t));
            //sideTriangles.ForEach(t => envTriangles.Add(t));         

            //// Create an aggregated list of Vertices representing the Roof envelope.
            //var enVertices = new List<Vertex>();
            //envTriangles.ForEach(t => enVertices.AddRange(t.Vertices));

            //// Construct the roof envelope in Elements.Geometry.mesh form.
            //var Envelope = new Elements.Geometry.Mesh();
            //envTriangles.ForEach(t => Envelope.AddTriangle(t));
            //enVertices.ForEach(v => Envelope.AddVertex(v));
            //Envelope.ComputeNormals();

            //// Construct serializable topSide mesh
            //var triangles = new List<Elements.Geometry.Triangle>();
            //var indices = new List<Vertex>();
            //var tsIV = topSide.ToIndexedVertices();



            //tsIV.triangles.ForEach(t => triangles.Add(new triangles(t)));
            //tsIV.vertices.ForEach(v => indices.Add(new vertices(v.index, v.isBoundary, v.position)));
            //var topSide = new Mesh(indices.ToList(), triangles.ToList(), BuiltInMaterials.Concrete);

            //// Construct serializable underside mesh           
            //triangles.Clear();
            //indices.Clear();
            //var usIV = underSide.ToIndexedVertices();
            //usIV.triangles.ForEach(t => triangles.Add(new triangles(t)));
            //usIV.vertices.ForEach(v => indices.Add(new vertices(v.index, v.isBoundary, v.position)));
            //var underside = new Elements.Mesh(triangles.ToList(), indices.ToList());

            //// Construct serializable envelope mesh           
            //triangles.Clear();
            //indices.Clear();
            //var enIV = Envelope.ToIndexedVertices();
            //enIV.triangles.ForEach(t => triangles.Add(new triangles(t)));
            //enIV.vertices.ForEach(v => indices.Add(new vertices(v.index, v.isBoundary, v.position)));
            //var envelope = new Elements.Mesh(triangles.ToList(), indices.ToList());

            ////Record roof high point from topSide mesh.
            //var highPoint = topSide.Points().OrderByDescending(p => p.Z).First().Z;

            var envelope = MakeEnvelope();
            var topside = MakeTopside();
            var underside = MakeUnderside();
            var underBoundary = Polygon.Rectangle(20.0, 20.0);
            var elevation = 10.0;
            var highPoint = 15.0;
            var area = 100.0;

            var roof =
                new Roof(
                    envelope,
                    topside,
                    underside,
                    underBoundary,
                    elevation,
                    highPoint,
                    input.Thickness,
                    area,
                    new Transform(),
                    BuiltInMaterials.Concrete,
                    null, false, Guid.NewGuid(), "Roof");
            var output = new RoofBySketchOutputs(area);


            output.Model.AddElement(new MeshElement(envelope, BuiltInMaterials.Concrete));
            output.Model.AddElement(roof);
            return output;
        }

        static Mesh MakeEnvelope()
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

        static Mesh MakeTopside()
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

        static Mesh MakeUnderside()
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