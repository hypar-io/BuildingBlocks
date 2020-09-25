using System;
using System.Linq;
using Elements;
using Elements.Geometry;
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
            var vertices = input.Mesh.Vertices.ToList();
            var topSide = new Elements.Geometry.Mesh();
            vertices.ForEach(v => topSide.AddVertex(v.Position));
            var area = 0.0;
            foreach (var triangle in input.Mesh.Triangles)
            {
                var triAng = new Triangle(topSide.Vertices[triangle.VertexIndices[0]],
                                          topSide.Vertices[triangle.VertexIndices[1]],
                                          topSide.Vertices[triangle.VertexIndices[2]]);
                topSide.AddTriangle(triAng);
                area += triAng.Area();
                
            }
            topSide.ComputeNormals();
            
            // Find the Mesh's lowest point and use the 
            // roof thickness to the set the Roof's underside elevation.
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
            var underSide = underBoundary.ToMesh();

            // Use the topSide Mesh's edgePoints and the lower Mesh's underPoints
            // to construct a series of triangles forming the sides of the Roof.
            var sideTriangles = new List<Triangle>();
            for(var i = 0; i < ePoints.Count; i++)
            {
                sideTriangles.Add(new Triangle(new Vertex(ePoints[i]),
                                  new Vertex(uPoints[i]),
                                  new Vertex(uPoints[(i + 1) % uPoints.Count])));
                sideTriangles.Add(new Triangle(new Vertex(ePoints[i]),
                                  new Vertex(uPoints[(i + 1) % uPoints.Count]),
                                  new Vertex(ePoints[(i + 1) % ePoints.Count])));
            }

            // Create an aggregated list of Triangles representing the Roof envelope.
            var envTriangles = new List<Triangle>();
            topSide.Triangles.ForEach(t => envTriangles.Add(t));
            underSide.Triangles.ForEach(t => envTriangles.Add(t));
            sideTriangles.ForEach(t => envTriangles.Add(t));         

            // Create an aggregated list of Vertices representing the Roof envelope.
            var enVertices = new List<Vertex>();
            envTriangles.ForEach(t => enVertices.AddRange(t.Vertices));

            // Construct the roof enevelope in Elements.Geometry.mesh form.
            var Envelope = new Elements.Geometry.Mesh();
            envTriangles.ForEach(t => Envelope.AddTriangle(t));
            enVertices.ForEach(v => Envelope.AddVertex(v));
            Envelope.ComputeNormals();

            // Construct serializable topside mesh
            var triangles = new List<triangles>();
            var indices = new List<vertices>();
            var tsIV = topSide.ToIndexedVertices();
            tsIV.triangles.ForEach(t => triangles.Add(new triangles(t)));
            tsIV.vertices.ForEach(v => indices.Add(new vertices(v.index, v.isBoundary, v.position)));
            var topside = new Elements.Mesh(triangles, indices);

            // Construct serializable underside mesh           
            triangles.Clear();
            indices.Clear();
            var usIV = underSide.ToIndexedVertices();
            usIV.triangles.ForEach(t => triangles.Add(new triangles(t)));
            usIV.vertices.ForEach(v => indices.Add(new vertices(v.index, v.isBoundary, v.position)));
            var underside = new Elements.Mesh(triangles, indices);

            // Construct serializable envelope mesh           
            triangles.Clear();
            indices.Clear();
            var enIV = Envelope.ToIndexedVertices();
            enIV.triangles.ForEach(t => triangles.Add(new triangles(t)));
            enIV.vertices.ForEach(v => indices.Add(new vertices(v.index, v.isBoundary, v.position)));
            var envelope = new Elements.Mesh(triangles, indices);

            //Record roof high point from topSide mesh.
            var highPoint = topSide.Points().OrderByDescending(p => p.Z).First().Z;

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
            output.Model.AddElement(new MeshElement(Envelope, BuiltInMaterials.Concrete));
            output.Model.AddElement(roof);
            return output;
        }
    }
}