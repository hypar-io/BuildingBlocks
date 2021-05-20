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

    public class GridLine
    {
        public Line Line;
        public string Name;

        public GridLine(Line line, string name = null)
        {
            this.Line = line;
            this.Name = name;
        }
    }

    public static class CustomGrids
    {
        private static double MinCircleRadius = 0.5;
        private static double CircleRadius = 1;

        private static Material GridlineMaterialU = new Material("GridlineU", Colors.Red);
        private static Material GridlineMaterialV = new Material("GridlineV", new Color(0, 0.5, 0, 1));

        private static Material LightweightBlueMaterial = new Material("Blue", new Color(0, 0.5, 1, 0.5));
        private static Material MagentaMaterial = new Material("Magenta", new Color(1, 0, 0.75, 0.5));

        /// <summary>
        ///
        /// </summary>
        /// <param name="model">The input model.</param>
        /// <param name="input">The arguments to the execution.</param>
        /// <returns>A CustomGridsOutputs instance containing computed results and the model with any new elements.</returns>
        public static CustomGridsOutputs Execute(Dictionary<string, Model> inputModels, CustomGridsInputs input)
        {
            var output = new CustomGridsOutputs();

            var envelopes = new List<Envelope>();
            inputModels.TryGetValue("Envelope", out var envelopeModel);
            if (envelopeModel != null)
            {
                Console.WriteLine("Got envelope model");
                envelopes.AddRange(envelopeModel.AllElementsOfType<Envelope>());
            }

            foreach (var gridArea in input.GridAreas)
            {
                var origin = gridArea.Orientation.Origin;
                var uDirection = gridArea.Orientation.XAxis;
                var vDirection = gridArea.Orientation.YAxis;

                Console.WriteLine($"Orientation: {gridArea.Orientation}");

                Console.WriteLine($"{origin}, {uDirection}, {vDirection}");

                if (input.ShowDebugGeometry)
                {
                    // Draw origin
                    output.Model.AddElement(new Line(origin - uDirection, origin + uDirection));
                    output.Model.AddElement(new Line(origin - vDirection, origin + vDirection));
                }

                var u = GetStandardizedRecords(gridArea.U, input);
                var v = GetStandardizedRecords(gridArea.V, input);

                var uSpacings = u.GridLines.Select(gl => gl.Spacing).ToList();
                var vSpacings = v.GridLines.Select(gl => gl.Spacing).ToList();

                var uMin = uSpacings.Count > 0 ? uSpacings.Min() : 10;
                var vMin = vSpacings.Count > 0 ? vSpacings.Min() : 10;

                CircleRadius = Math.Max(MinCircleRadius, Math.Max(uMin, vMin) * 0.3);

                var uDivisions = GetDivisions(origin, uDirection, u);
                var vDivisions = GetDivisions(origin, vDirection, v);

                var uPoints = uDivisions.Select(division => division.Point).ToList();
                var vPoints = vDivisions.Select(division => division.Point).ToList();

                var boundaries = new List<List<Polygon>>();
                var grids = new List<Grid2d>();

                var gridPolygon = new Polygon(new List<Vector3>() { origin, origin + uDirection, origin + vDirection });

                var gridPtsMin = new Vector3(Math.Min(uPoints.FirstOrDefault().X, vPoints.FirstOrDefault().X), Math.Min(uPoints.FirstOrDefault().Y, vPoints.FirstOrDefault().Y));
                var gridPtsMax = new Vector3(Math.Max(uPoints.LastOrDefault().X, vPoints.LastOrDefault().X), Math.Max(uPoints.LastOrDefault().Y, vPoints.LastOrDefault().Y));

                gridPtsMin.X = Math.Min(gridPtsMin.X, origin.X);
                gridPtsMin.Y = Math.Min(gridPtsMin.Y, origin.Y);

                gridPtsMax.X = Math.Max(gridPtsMax.X, origin.X);
                gridPtsMax.Y = Math.Max(gridPtsMax.Y, origin.Y);

                if (envelopes.Count() > 0)
                {
                    var polygons = envelopes.Select(e => e.Profile.Perimeter).ToList();
                    polygons.Add(gridPolygon);
                    var unions = Polygon.UnionAll(polygons).ToList();
                    var boundary = PolygonFromAlignedBoundingBox2d(unions.Select(u => u.Vertices).SelectMany(x => x), new List<Line>() { new Line(origin, origin + uDirection), new Line(origin, origin + vDirection) });
                    boundaries.Add(new List<Polygon>() { boundary });
                }
                else
                {
                    // use points min and max
                    boundaries.Add(new List<Polygon>() { Polygon.Rectangle(gridPtsMin, gridPtsMax) });
                }

                var gridNodes = new List<GridNode>();

                foreach (var boundaryList in boundaries)
                {
                    foreach (var boundary in boundaryList)
                    {
                        var grid = MakeGrid(boundary, origin, uDirection, vDirection, uPoints, vPoints);
                        var uGridLines = DrawLines(output.Model, origin, uDivisions, grid.V, boundary, GridlineMaterialU);
                        var vGridLines = DrawLines(output.Model, origin, vDivisions, grid.U, boundary, GridlineMaterialV);
                        grids.Add(grid);

                        if (input.ShowDebugGeometry)
                        {
                            var uCurve = new Line(origin, origin + uDirection);
                            var vCurve = new Line(origin, origin + vDirection);

                            output.Model.AddElement(new ModelCurve(uCurve, MagentaMaterial));
                            output.Model.AddElement(new ModelCurve(vCurve, MagentaMaterial));
                        }

                        foreach (var uGridLine in uGridLines)
                        {
                            foreach (var vGridLine in vGridLines)
                            {
                                if (uGridLine.Line.Intersects(vGridLine.Line, out var intersection))
                                {
                                    var transform = new Transform(intersection);
                                    gridNodes.Add(new GridNode(transform, Guid.NewGuid(), $"{uGridLine.Name}{vGridLine.Name}"));

                                    if (input.ShowDebugGeometry)
                                    {
                                        DrawDebugPoint(output.Model, intersection, LightweightBlueMaterial);
                                    }
                                }
                                else
                                {
                                    Console.WriteLine($"No intersection between two gridlines found: {uGridLine.Name}, {vGridLine.Name}");
                                }
                            }
                        }

                        if (input.ShowDebugGeometry)
                        {
                            foreach (var cell in grid.GetCells())
                            {
                                var polygon = (Polygon)cell.GetCellGeometry();
                                foreach (var vertex in polygon.Vertices)
                                {
                                    DrawDebugPoint(output.Model, vertex, MagentaMaterial);
                                }
                                output.Model.AddElement(new ModelCurve(cell.GetCellGeometry(), LightweightBlueMaterial));
                            }
                            output.Model.AddElement(new ModelCurve(boundary));

                            foreach (var pt in vPoints)
                            {
                                DrawDebugPoint(output.Model, pt, MagentaMaterial);
                            }
                            foreach (var pt in uPoints)
                            {
                                DrawDebugPoint(output.Model, pt, MagentaMaterial);
                            }
                        }
                    }
                }

                output.Model.AddElements(grids.Select(grid =>
                {
                    return new Grid2dElement(grid, gridNodes, Guid.NewGuid(), gridArea.Name);
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

        private static GridLine DrawLine(Model model, Line line, Material material, string name)
        {
            var circleCenter = line.Start - (line.End - line.Start).Unitized() * CircleRadius;

            model.AddElement(new Elements.GridLine(new Polyline(new List<Vector3>() { line.Start, line.End }), Guid.NewGuid(), name));
            model.AddElement(new ModelCurve(line, material, name: name));
            model.AddElement(new ModelCurve(new Circle(circleCenter, CircleRadius), material));
            model.AddElement(new LabelDot(circleCenter, name));
            return new GridLine(line, name);
        }

        private static List<GridGuide> GetDivisions(Vector3 origin, Vector3 gridDir, U u)
        {
            var gridGuides = new List<GridGuide>();
            var curPoint = origin;
            var sectionIdx = 0;
            foreach (var gridline in u.GridLines)
            {
                curPoint = curPoint + gridDir * gridline.Spacing;
                gridGuides.Add(new GridGuide(curPoint, GetName(u.Name, sectionIdx)));
                sectionIdx += 1;
            }
            return gridGuides;
        }

        private static List<GridLine> DrawLines(Model model, Vector3 origin, List<GridGuide> gridGuides, Grid1d opposingGrid1d, Polygon bounds, Material material)
        {
            var baseLine = new Line(opposingGrid1d.Curve.PointAt(0), opposingGrid1d.Curve.PointAt(1));

            var startExtend = origin - baseLine.Start;
            var endExtend = origin - baseLine.End;

            List<GridLine> gridLines = new List<GridLine>();

            foreach (var gridGuide in gridGuides)
            {
                var line = new Line(gridGuide.Point - startExtend, gridGuide.Point - endExtend);
                var gridLine = DrawLine(model, line, material, gridGuide.Name);
                gridLines.Add(gridLine);
            }

            return gridLines;
        }

        private static Polygon PolygonFromAlignedBoundingBox2d(IEnumerable<Vector3> points, List<Line> segments)
        {
            var hull = ConvexHull.FromPoints(points);
            var minBoxArea = double.MaxValue;
            BBox3 minBox = new BBox3();
            Transform minBoxXform = new Transform();
            foreach (var edge in segments)
            {
                var edgeVector = edge.End - edge.Start;
                var xform = new Transform(Vector3.Origin, edgeVector, Vector3.ZAxis, 0);
                var invertedXform = new Transform(xform);
                invertedXform.Invert();
                var transformedPolygon = hull.TransformedPolygon(invertedXform);
                var bbox = new BBox3(transformedPolygon.Vertices);
                var bboxArea = (bbox.Max.X - bbox.Min.X) * (bbox.Max.Y - bbox.Min.Y);
                if (bboxArea < minBoxArea)
                {
                    minBoxArea = bboxArea;
                    minBox = bbox;
                    minBoxXform = xform;
                }
            }
            var xy = new Plane(Vector3.Origin, Vector3.ZAxis);
            var boxRect = Polygon.Rectangle(minBox.Min.Project(xy), minBox.Max.Project(xy));
            return boxRect.TransformedPolygon(minBoxXform);
        }

        private static void DrawDebugPoint(Model model, Vector3 location, Material material)
        {
            var transform = new Transform(location);
            model.AddElement(new Panel(new Circle(Vector3.EPSILON * 2).ToPolygon(), material, transform));
            model.AddElement(new Panel(new Circle(0.003).ToPolygon(), material, transform));
        }

        /// <summary>
        /// Standardizes records so that they all use spacing and not location.
        /// Performs the duplication over quantity.
        /// </summary>
        /// <param name="u"></param>
        /// <param name="input"></param>
        /// <returns></returns>
        private static U GetStandardizedRecords(U u, CustomGridsInputs input)
        {
            var gridlines = new List<GridLines>();

            if (u.Spacing != null && u.Spacing.Count > 0)
            {
                // Deprecation support
                foreach (var spacing in u.Spacing)
                {
                    gridlines.Add(new GridLines(0, spacing, 1));
                }
            }
            else if (input.Mode == CustomGridsInputsMode.Absolute)
            {
                var sortedGridlines = u.GridLines.OrderBy(gl => gl.Location).ToList();
                for (var i = 0; i < sortedGridlines.Count; i++)
                {
                    var gl = sortedGridlines[i];
                    if (i == 0)
                    {
                        gridlines.Add(new GridLines(0, gl.Location, 1));
                    }
                    else
                    {
                        var prev = sortedGridlines[i - 1];
                        gridlines.Add(new GridLines(0, gl.Location - prev.Location, 1));
                    }

                }
            }
            else
            {

                foreach (var gl in u.GridLines)
                {
                    for (int i = 0; i < gl.Quantity; i++)
                    {
                        gridlines.Add(new GridLines(0, gl.Spacing, 1));
                    }
                }
            }

            return new U(u.Name, u.Spacing, gridlines);
        }

        private static U GetStandardizedRecords(V v, CustomGridsInputs input)
        {
            var vAsU = new U(v.Name, v.Spacing, v.GridLines);
            return GetStandardizedRecords(vAsU, input);
        }
    }
}