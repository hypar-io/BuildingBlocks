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
        private static Material MagentaMaterial = new Material("Magenta", Colors.Magenta);

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

            input = GetInput(); // TEMP TEMP TEMP
            input.ShowDebugGeometry = true;

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

                            var lines = new List<Line>() { new Line(uCurve.Start, uCurve.End), new Line(vCurve.Start, vCurve.End) };

                            ExpandLinesToBounds(new BBox3(boundary), lines, output.Model);

                            foreach (var line in lines)
                            {
                                output.Model.AddElement(new ModelCurve(line, MagentaMaterial));
                            }
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
                                        // output.Model.AddElement(new ModelCurve(new Circle(0.003).ToPolygon(), LightweightBlueMaterial, transform));
                                        // output.Model.AddElement(new ModelCurve(new Circle(0.25).ToPolygon(), LightweightBlueMaterial, transform));
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
                            var inputTransform = new Transform(new Vector3(0, 0, 0));
                            foreach (var cell in grid.GetCells())
                            {
                                // output.Model.AddElement(new ModelCurve(cell.GetCellGeometry(), transform: inputTransform));
                            }
                            output.Model.AddElement(new ModelCurve(boundary, transform: inputTransform));

                            // output.Model.AddElement(new ModelCurve(grid.U.Curve, LightweightBlueMaterial, inputTransform));
                            // output.Model.AddElement(new ModelCurve(grid.V.Curve, LightweightBlueMaterial, inputTransform));

                            foreach (var pt in vPoints)
                            {
                                output.Model.AddElement(new ModelCurve(new Circle(0.003).ToPolygon(), MagentaMaterial, new Transform(pt)));
                                output.Model.AddElement(new ModelCurve(new Circle(0.25).ToPolygon(), MagentaMaterial, new Transform(pt)));
                            }
                            foreach (var pt in uPoints)
                            {
                                output.Model.AddElement(new ModelCurve(new Circle(0.003).ToPolygon(), MagentaMaterial, new Transform(pt)));
                                output.Model.AddElement(new ModelCurve(new Circle(0.25).ToPolygon(), MagentaMaterial, new Transform(pt)));
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
            foreach (var spacing in u.Spacing)
            {
                curPoint = curPoint + gridDir * spacing;
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

        public static CustomGridsInputs GetInput()
        {
            var inputText = @"
            {
  ""Show Debug Geometry"": true,
  ""Grid Areas"": [
    {
      ""Orientation"": {
        ""Matrix"": {
          ""Components"": [
            0.9999947633971941,
            -0.009614940148020392,
            0,
            -10.992100150908389,
            -0.003236229007759388,
            -0.9999537753946179,
            0,
            17.971426034076142,
            0,
            0,
            1.0000000000000004,
            0
          ]
        }
      },
      ""U"": {
        ""Spacing"": [
          0.6,
          11.2,
          25.8,
          1.2
        ],
        ""Name"": ""{A}""
      },
      ""V"": {
        ""Spacing"": [
          1.2,
          7.2,
          7.2,
          7.2,
          7.2,
          7.2,
          7.2,
          7.2,
          1.2
        ],
        ""Name"": ""{1}""
      },
      ""Name"": ""Grid""
    }
  ],
  ""model_input_keys"": {
    ""Envelope"": ""bd950da8-7030-470e-81da-caca9d313608_0c8d0526-9490-4d53-896b-88a1515de583_elements.zip""
  }
}
            ";
            return Newtonsoft.Json.JsonConvert.DeserializeObject<CustomGridsInputs>(inputText);
        }

        private static List<Line> ExpandLinesToBounds(BBox3 bounds, List<Line> lines, Model model)
        {
            var boundary = Polygon.Rectangle(bounds.Min, bounds.Max);

            for (var i = 0; i < lines.Count(); i++)
            {
                lines[i] = lines[i].ExtendTo(boundary, true, true);
            }

            var new1 = ExtendLineSkewed(bounds, lines[0], lines[1], model);
            var new2 = ExtendLineSkewed(bounds, lines[1], new1, model);

            new1 = ExtendLineSkewed(bounds, new1, new2, model);
            new2 = ExtendLineSkewed(bounds, new2, new1, model);

            lines[0] = new1;
            lines[1] = new2;

            return lines;
        }

        /// <summary>
        /// Extend a line to a bounding box along a second line,
        /// making sure this line extends to the boundary at the endpoints of second line to account for its skew.
        /// Used to make sure U and V guide lines extend out far enough to encompass a boundary.
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="line"></param>
        /// <param name="possiblySkewedLine"></param>
        /// <returns></returns>
        private static Line ExtendLineSkewed(BBox3 bounds, Line line, Line possiblySkewedLine, Model model)
        {
            var boundary = Polygon.Rectangle(bounds.Min, bounds.Max);

            var newLine = new Line(line.Start, line.End);

            if (newLine.Intersects(possiblySkewedLine, out var intersection))
            {
                // // move to start and extend
                // var toStart = possiblySkewedLine.Start - intersection;
                // newLine = newLine.TransformedLine(new Transform(toStart));
                // newLine = newLine.ExtendTo(boundary, true, true);

                var beforeRay = (newLine.End - newLine.Start).Unitized();

                // move to end and extend
                var toEnd = possiblySkewedLine.End - intersection;
                newLine = newLine.TransformedLine(new Transform(toEnd));
                newLine = newLine.ExtendTo(boundary, true, true);

                var afterRay = (newLine.End - newLine.Start).Unitized();

                if (!beforeRay.Equals(afterRay)) {
                    var transformedLine = (new Line(line.Start, line.End)).TransformedLine(new Transform(toEnd));
                    Console.WriteLine($"Before and after");
                    Console.WriteLine($"-- {beforeRay}");
                    Console.WriteLine($"-- {afterRay}");
                }

            // move back to original
            var toBeginning = intersection - possiblySkewedLine.End;
                newLine = newLine.TransformedLine(new Transform(toBeginning));
            }

            return newLine;
        }
    }
}