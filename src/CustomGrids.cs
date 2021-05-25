using Elements;
using Elements.Geometry;
using Elements.Spatial;
using System.Collections.Generic;
using System;
using System.Linq;
using Newtonsoft.Json;

namespace CustomGrids
{
    public static class CustomGrids
    {
        private static double MinCircleRadius = 0.5;
        private static double CircleRadius = 1;

        private static Material GridlineMaterialU = new Material("GridlineU", Colors.Red);
        private static Material GridlineMaterialV = new Material("GridlineV", new Color(0, 0.5, 0, 1));

        private static Material Grid2dElementMaterial = new Material("Grid2dElement", new Color(1, 1, 1, 0.01));

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

            var envelopePolygons = new List<Polygon>();

            if (envelopes.Count == 0 && input.Mode == CustomGridsInputsMode.Typical)
            {
                output.Errors.Add("When using typical spacing, an envelope is required to calculate your number of gridlines. If you do not have an envelope available, please use Absolute or Relative spacing.");
                return output;
            }
            else
            {
                envelopePolygons = envelopes.Select(e => (Polygon)e.Profile.Perimeter.Transformed(e.Transform)).ToList();
            }

            foreach (var gridArea in input.GridAreas)
            {
                output.Model.AddElements(CreateGridArea(input, output, envelopePolygons, gridArea));
            }
            return output;
        }

        private static Transform GetOrigin(GridAreas gridArea, List<Polygon> envelopePolygons, CustomGridsInputs input)
        {
            var transform = new Transform();

            // Automatically assume origin from envelope shape
            if (envelopePolygons.Count > 0)
            {
                var bounds = Polygon.FromAlignedBoundingBox2d(envelopePolygons.SelectMany(polygon => polygon.Vertices).Select(vertex => new Vector3(vertex.X, vertex.Y)));
                var axes = CalculateAxes(bounds);
                transform = new Transform(axes.x.Start, (axes.x.End - axes.x.Start).Unitized(), (axes.y.End - axes.y.Start).Unitized(), Vector3.ZAxis);
            }

            // Use deprecated manual origin, if it exists
            if (gridArea.Orientation != null && !gridArea.Orientation.Equals(new Transform()))
            {
                transform = gridArea.Orientation;
            }

            // Apply origin overrides if set
            if (input.Overrides != null)
            {
                foreach (var overrideOrigin in input.Overrides.GridOrigins)
                {
                    if (overrideOrigin.Identity.Name == gridArea.Name)
                    {
                        transform = overrideOrigin.Value.Transform;
                    }
                }
            }
            return transform;
        }

        private static List<Grid2dElement> CreateGridArea(
            CustomGridsInputs input,
            CustomGridsOutputs output,
            List<Polygon> envelopePolygons,
            GridAreas gridArea
        )
        {
            var transform = GetOrigin(gridArea, envelopePolygons, input);
            var origin = transform.Origin;
            var uDirection = transform.XAxis;
            var vDirection = transform.YAxis;

            if (input.ShowDebugGeometry)
            {
                // Draw origin
                output.Model.AddElement(new Line(origin - uDirection, origin + uDirection));
                output.Model.AddElement(new Line(origin - vDirection, origin + vDirection));
            }

            var standardizedRecords = GetStandardizedRecords(output.Model, gridArea.U, gridArea.V, input, envelopePolygons, origin, uDirection, vDirection);

            var u = standardizedRecords.u;
            var v = standardizedRecords.v;

            CircleRadius = GetCircleRadius(u, v);

            var uDivisions = GetDivisions(origin, uDirection, u);
            var vDivisions = GetDivisions(origin, vDirection, v);

            var uPoints = uDivisions.Select(division => division.Point).ToList();
            var vPoints = vDivisions.Select(division => division.Point).ToList();

            var boundaries = new List<List<Polygon>>();
            var grids = new List<(Grid2d grid, Polygon boundary)>();

            var gridPoints = new List<Vector3>();

            foreach (var uPoint in uPoints)
            {
                foreach (var vPoint in vPoints)
                {
                    var offset = vPoint - uPoints[0];
                    var gridpoint = uPoint + offset;
                    gridPoints.Add(gridpoint);
                    if (input.ShowDebugGeometry)
                    {
                        output.Model.AddElement(new Panel(new Circle().ToPolygon(), null, new Transform(gridpoint)));
                    }
                }
            }

            if (gridPoints.Count == 0)
            {
                throw new Exception("No grids were able to be calculated from the given inputs.");
            }

            Polygon gridPolygon = null;

            try
            {
                gridPolygon = Polygon.FromAlignedBoundingBox2d(gridPoints);
            }
            catch (Exception _)
            {
                gridPolygon = Polygon.FromAlignedBoundingBox2d(new List<Vector3>() { origin, origin + uDirection, origin + vDirection });
            }

            if (input.ShowDebugGeometry)
            {
                output.Model.AddElement(new ModelCurve(gridPolygon));
            }

            if (envelopePolygons.Count() > 0)
            {
                var polygons = envelopePolygons.Concat(new List<Polygon>() { gridPolygon }).ToList();
                var unions = Polygon.UnionAll(polygons).ToList();
                var boundary = PolygonFromAlignedBoundingBox2d(unions.Select(u => u.Vertices).SelectMany(x => x), new List<Line>() { new Line(origin, origin + uDirection), new Line(origin, origin + vDirection) });
                boundaries.Add(new List<Polygon>() { boundary });
            }
            else
            {
                // use points min and max
                boundaries.Add(new List<Polygon>() { gridPolygon });
            }

            var gridNodes = new List<GridNode>();

            foreach (var boundary in boundaries.SelectMany(boundaryList => boundaryList).ToList())
            {
                var grid = MakeGrid(boundary, origin, uDirection, vDirection, uPoints, vPoints);
                var uGridLines = DrawLines(output.Model, origin, uDivisions, grid.V, boundary, GridlineMaterialU);
                var vGridLines = DrawLines(output.Model, origin, vDivisions, grid.U, boundary, GridlineMaterialV);
                grids.Add((grid: grid, boundary: boundary));

                if (input.ShowDebugGeometry)
                {
                    var uCurve = new Line(origin, origin + uDirection);
                    var vCurve = new Line(origin, origin + vDirection);

                    output.Model.AddElement(new ModelCurve(uCurve, Debug.MagentaMaterial));
                    output.Model.AddElement(new ModelCurve(vCurve, Debug.MagentaMaterial));
                }

                foreach (var uGridLine in uGridLines)
                {
                    foreach (var vGridLine in vGridLines)
                    {
                        if (uGridLine.Line.Intersects(vGridLine.Line, out var intersection))
                        {
                            var gridNodeTransform = new Transform(intersection);
                            gridNodes.Add(new GridNode(gridNodeTransform, Guid.NewGuid(), $"{uGridLine.Name}{vGridLine.Name}"));

                            if (input.ShowDebugGeometry)
                            {
                                Debug.DrawPoint(output.Model, intersection, Debug.LightweightBlueMaterial);
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
                    Debug.DrawGrid(output.Model, grid, uPoints, vPoints);
                }
            }

            return grids.Select(grid =>
            {
                var transform = new Transform(origin, uDirection, vDirection, Vector3.ZAxis);
                var invert = new Transform(origin, uDirection, vDirection, Vector3.ZAxis);
                invert.Invert();
                var boundary = (Polygon)grid.boundary.Transformed(invert);
                var rep = new Representation(new List<Elements.Geometry.Solids.SolidOperation>() { new Elements.Geometry.Solids.Lamina(boundary, false) });
                return new Grid2dElement(grid.grid, gridNodes, transform, Grid2dElementMaterial, rep, false, Guid.NewGuid(), gridArea.Name);
            }).ToList();
        }

        private static double GetCircleRadius(U u, U v)
        {
            var uSpacings = u.GridLines.Select(gl => gl.Spacing).ToList();
            var vSpacings = v.GridLines.Select(gl => gl.Spacing).ToList();

            var uMin = uSpacings.Count > 0 ? uSpacings.Min() : 10;
            var vMin = vSpacings.Count > 0 ? vSpacings.Min() : 10;

            return Math.Max(MinCircleRadius, Math.Max(uMin, vMin) * 0.3);
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

        private static (U u, U v) GetStandardizedRecords(Model model, U u, V v, CustomGridsInputs input, List<Polygon> envelopePolygons, Vector3 origin, Vector3 uDirection, Vector3 vDirection)
        {
            if (input.Mode == CustomGridsInputsMode.Typical)
            {
                var bounds = Polygon.FromAlignedBoundingBox2d(envelopePolygons.SelectMany(polygon => polygon.Vertices).Select(vertex => new Vector3(vertex.X, vertex.Y)));

                var xAxis = new Line(origin, origin + uDirection);
                xAxis = xAxis.ExtendTo(bounds, true, true);

                var yAxis = new Line(origin, origin + vDirection);
                yAxis = yAxis.ExtendTo(bounds, true, true);

                if (input.ShowDebugGeometry)
                {
                    model.AddElement(new ModelCurve(bounds));
                }
                return (u: GetStandardizedRecords(u, input, xAxis.Length()), v: GetStandardizedRecords(v, input, yAxis.Length()));
            }
            else
            {
                return (u: GetStandardizedRecords(u, input, 0), v: GetStandardizedRecords(v, input, 0));
            }
        }

        /// <summary>
        /// Standardizes records so that they all use relative spacing and not location,
        /// and have one gridline per instance (all quantities 1 : performs the duplication over quantity)
        /// </summary>
        /// <param name="u"></param>
        /// <param name="input"></param>
        /// <returns></returns>
        private static U GetStandardizedRecords(U u, CustomGridsInputs input, double axisLength)
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
            else if (input.Mode == CustomGridsInputsMode.Typical)
            {
                gridlines.Add(new GridLines(0, u.OffsetStart, 1));
                var spaceInBetween = axisLength - u.OffsetStart - u.OffsetEnd;

                if (spaceInBetween > 0)
                {
                    var numSpaces = Math.Max(1, Math.Floor(spaceInBetween / u.TargetTypicalSpacing));
                    var interval = spaceInBetween / numSpaces;
                    for (var i = 0; i < numSpaces; i++)
                    {
                        gridlines.Add(new GridLines(0, interval, 1));
                    }
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

            return new U(u.Name, u.Spacing, gridlines, 0, 0, 0);
        }

        private static U GetStandardizedRecords(V v, CustomGridsInputs input, double axisLength)
        {
            var vAsU = new U(v.Name, v.Spacing, v.GridLines, v.TargetTypicalSpacing, v.OffsetStart, v.OffsetEnd);
            return GetStandardizedRecords(vAsU, input, axisLength);
        }

        private static (Line x, Line y) CalculateAxes(Polygon alignedBoundingBox2d)
        {
            var sortedPoints = alignedBoundingBox2d.Vertices.OrderBy(pt => pt.X).ThenBy(pt => pt.Y).ToList();
            var origin = sortedPoints[0];

            (Line x, Line y) axes = (null, null);

            foreach (var segment in alignedBoundingBox2d.Segments())
            {
                var startsOnOrigin = segment.Start.Equals(origin);
                var endsOnOrigin = segment.End.Equals(origin);
                Line axis = null;
                if (startsOnOrigin)
                {
                    axis = segment;
                }
                else if (endsOnOrigin)
                {
                    axis = new Line(origin, segment.Start);
                }
                else
                {
                    continue;
                }

                var angle = new Vector3(axis.End - axis.Start).AngleTo(Vector3.XAxis);

                if (angle < 45)
                {
                    axes.x = axis;
                }
                else
                {
                    axes.y = axis;
                }
            }
            return axes;
        }
    }
}