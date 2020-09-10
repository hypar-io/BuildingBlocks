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
            var mesh = new Elements.Geometry.Mesh();
            input.Mesh.Vertices.ToList().ForEach(v => mesh.AddVertex(v.Position));
            foreach (var triangle in input.Mesh.Triangles)
            {
                mesh.AddTriangle(mesh.Vertices[triangle.VertexIndices[0]], 
                                 mesh.Vertices[triangle.VertexIndices[1]], 
                                 mesh.Vertices[triangle.VertexIndices[2]]);
            }
            mesh.ComputeNormals();
            var area = 0.0;
            mesh.Triangles.ForEach(t => area += t.Area());
            var output = new RoofBySketchOutputs(area);
            var roofMatl = new Material("roof", new Color(0.7, 0.7, 0.7, 1.0), 0.0, 0.0, doubleSided: true);
            var lineMatl = new Material("roof", new Color(1.0, 0.0, 0.0, 1.0), 0.0, 0.0, doubleSided: true);
            output.Model.AddElement(new MeshElement(mesh, roofMatl, new Transform()));
            //GetLowLines(mesh, input).ForEach(e => output.Model.AddElement(new ModelCurve(e, lineMatl)));
            GetRoofSections(mesh, GetLowLines(mesh, input), input).ForEach(r => output.Model.AddElement(r));
            return output;
        }

        private static List<Line> GetLowLines(Elements.Geometry.Mesh mesh, RoofBySketchInputs input)
        {
            var lowLines = new List<Line>();
            foreach(var edge in mesh.Edges())
            {
                if (mesh.IsValley(edge, Vector3.ZAxis))
                {
                    lowLines.Add(edge);
                }
            }
            return lowLines;
        }

        private static List<DrainableRoofSection> GetRoofSections(Elements.Geometry.Mesh mesh, List<Line> lowLines, RoofBySketchInputs input)
        {
            var edges = mesh.Edges();
            var sections = new List<DrainableRoofSection>();
            foreach (var line in lowLines)
            {
                var points = new List<Vector3>();
                var jLines = line.SharesEndpointWith(edges);
                foreach (var jLine in jLines)
                {
                    if (jLine.Start.IsAlmostEqualTo(line.Start) ||
                        jLine.Start.IsAlmostEqualTo(line.End))
                    {
                        points.Add(new Vector3(jLine.End.X, jLine.End.Y));
                        continue;
                    }
                    points.Add(new Vector3(jLine.Start.X, jLine.Start.Y));
                }
                points = Shaper.SortRadial(points, line.Midpoint());
                points.Reverse();
                var boundary = new Polygon(points).TransformedPolygon(new Transform(0.0, 0.0, 20.0));
                sections.Add(new DrainableRoofSection(boundary, new List<Line> { line.TransformedLine(new Transform()) }, null, BuiltInMaterials.Concrete, null, false, Guid.NewGuid(), ""));
            }
            return sections;
        }
    }
}