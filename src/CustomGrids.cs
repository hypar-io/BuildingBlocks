using Elements;
using Elements.Geometry;
using Elements.Spatial;
using System.Collections.Generic;
using System;
using System.Linq;
using Newtonsoft.Json;

namespace CustomGrids
{
    public class GridGuide
    {
        public Vector3 Point;
        public string Name;

        public GridGuide(Vector3 point, string name = null)
        {
            this.Point = point;
            this.Name = name;
        }
    }

    public static class CustomGrids
    {
        private static double MinCircleRadius = 0.5;
        private static double CircleRadius = 1;

        private static Material GridlineMaterial = new Material("Gridline", Colors.Red);

        private static Model DebugModel = null;

        /// <summary>
        ///
        /// </summary>
        /// <param name="model">The input model.</param>
        /// <param name="input">The arguments to the execution.</param>
        /// <returns>A CustomGridsOutputs instance containing computed results and the model with any new elements.</returns>
        public static CustomGridsOutputs Execute(Dictionary<string, Model> inputModels, CustomGridsInputs input)
        {
            var output = new CustomGridsOutputs();

            DebugModel = output.Model;

            var envelopes = new List<Envelope>();
            inputModels.TryGetValue("Envelope", out var envelopeModel);
            if (envelopeModel != null)
            {
                Console.WriteLine("Got envelope model");
                envelopes.AddRange(envelopeModel.AllElementsOfType<Envelope>());
            }

            foreach (var gridArea in input.GridAreas)
            {

                if (gridArea.Orientation.Vertices.Count < 3)
                {
                    throw new Exception("Your grid orientation must have 3 vertices");
                }

                var origin = gridArea.Orientation.Vertices[0];
                var uDirection = (gridArea.Orientation.Vertices[1] - gridArea.Orientation.Vertices[0]).Unitized();
                var vDirection = (gridArea.Orientation.Vertices[2] - gridArea.Orientation.Vertices[0]).Unitized();

                if (input.ShowDebugGeometry)
                {
                    // Draw origin
                    output.Model.AddElement(new Line(origin - uDirection, origin + uDirection));
                    output.Model.AddElement(new Line(origin - vDirection, origin + vDirection));
                }

                var u = gridArea.U;
                var v = new U(gridArea.V.Name, gridArea.V.Spacing);

                var uMin = u.Spacing.Count > 0 ? u.Spacing.Min() : 10;
                var vMin = v.Spacing.Count > 0 ? v.Spacing.Min() : 10;

                CircleRadius = Math.Max(MinCircleRadius, Math.Max(uMin, vMin) * 0.3);

                var uDivisions = GetDivisions(origin, uDirection, u);
                var vDivisions = GetDivisions(origin, vDirection, v);

                var uPoints = uDivisions.Select(division => division.Point).ToList();
                var vPoints = vDivisions.Select(division => division.Point).ToList();

                var boundaries = new List<List<Polygon>>();
                var grids = new List<Grid2d>();

                var gridPolygon = new Polygon(gridArea.Orientation.Vertices);

                if (envelopes.Count() > 0)
                {
                    var polygons = envelopes.Select(e => e.Profile.Perimeter).ToList();
                    polygons.Add(gridPolygon);
                    var unions = Polygon.UnionAll(polygons).ToList();
                    var bbox = new BBox3(unions);
                    var boundary = Polygon.Rectangle(bbox.Min, bbox.Max);
                    boundaries.Add(new List<Polygon>() { boundary });
                }
                else
                {
                    // use points min and max
                    var min = new Vector3(Math.Min(uPoints.FirstOrDefault().X, vPoints.FirstOrDefault().X), Math.Min(uPoints.FirstOrDefault().Y, vPoints.FirstOrDefault().Y));
                    var max = new Vector3(Math.Max(uPoints.LastOrDefault().X, vPoints.LastOrDefault().X), Math.Max(uPoints.LastOrDefault().Y, vPoints.LastOrDefault().Y));

                    min.X = Math.Min(min.X, origin.X);
                    min.Y = Math.Min(min.Y, origin.Y);

                    max.X = Math.Min(max.X, origin.X);
                    max.Y = Math.Min(max.Y, origin.Y);

                    boundaries.Add(new List<Polygon>() { Polygon.Rectangle(min, max) });
                }

                foreach (var boundaryList in boundaries)
                {
                    foreach (var boundary in boundaryList)
                    {
                        var grid = MakeGrid(boundary, origin, uDirection, vDirection, uPoints, vPoints);
                        DrawLines(output.Model, origin, uDivisions, grid.V, boundary);
                        DrawLines(output.Model, origin, vDivisions, grid.U, boundary);
                        grids.Add(grid);

                        if (input.ShowDebugGeometry)
                        {
                            // foreach (var cell in grid.GetCells())
                            // {
                            //     output.Model.AddElement(new ModelCurve(cell.GetCellGeometry()));
                            // }
                            output.Model.AddElements(new ModelCurve(boundary));

                        }

                    }
                }

                output.Model.AddElements(grids.Select(grid =>
                {
                    return new Grid2dElement(grid, Guid.NewGuid(), gridArea.Name);
                }));
            }
            return output;
        }

        private static Grid2d MakeGrid(Polygon polygon, Vector3 origin, Vector3 uDir, Vector3 vDir, List<Vector3> uPoints, List<Vector3> vPoints)
        {
            var grid2d = new Grid2d(polygon, origin, uDir, vDir);

            grid2d.U.SplitAtPoints(uPoints);
            grid2d.V.SplitAtPoints(vPoints);

            return grid2d;
        }

        private static string GetName(string namingPattern, int idx)
        {
            var name = $"{namingPattern}-{idx}";

            if (namingPattern == "{A}")
            {
                name = ((char)(idx + 65)).ToString(); // nth letter of alphabet
            }
            if (namingPattern == "{1}")
            {
                name = (idx + 1).ToString();
            }
            return name;
        }

        private static void DrawLine(Model model, Line line, Material material, string name)
        {
            var circleCenter = line.Start - (line.End - line.Start).Unitized() * CircleRadius;

            model.AddElement(new GridLine(new Polyline(new List<Vector3>() { line.Start, line.End }), Guid.NewGuid(), name));
            model.AddElement(new ModelCurve(line, material, name: name));
            model.AddElement(new ModelCurve(new Circle(circleCenter, CircleRadius), GridlineMaterial));
            model.AddElement(new LabelDot(circleCenter, name));
        }

        private static List<GridGuide> GetDivisions(Vector3 origin, Vector3 gridDir, U u)
        {
            var gridGuides = new List<GridGuide>();
            var curPoint = origin;
            var sectionIdx = 0;
            foreach (var spacing in u.Spacing)
            {
                curPoint = curPoint + gridDir * spacing;
                gridGuides.Add(new GridGuide(curPoint, GetName(u.Name, sectionIdx)));
                sectionIdx += 1;
            }
            return gridGuides;
        }

        private static void DrawLines(Model model, Vector3 origin, List<GridGuide> gridGuides, Grid1d opposingGrid1d, Polygon bounds)
        {
            var baseLine = new Line(opposingGrid1d.Curve.PointAt(0), opposingGrid1d.Curve.PointAt(1));

            var startExtend = origin - baseLine.Start;
            var endExtend = origin - baseLine.End;

            foreach (var gridGuide in gridGuides)
            {
                var line = new Line(gridGuide.Point - startExtend, gridGuide.Point - endExtend);
                DrawLine(model, line, GridlineMaterial, gridGuide.Name);
            }
        }
    }
}