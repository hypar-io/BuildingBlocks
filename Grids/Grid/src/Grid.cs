using Elements;
using Elements.Geometry;
using Elements.Spatial;
using System.Collections.Generic;
using System;
using System.Linq;

namespace Grid
{
    public static class Grid
    {
        private const string parametrizedPositionPropertyName = "ParametrizedPosition";
        private const string axisPropertyName = "Axis";
        private const double pointsInMeter = 2835;

        private static double MinCircleRadius = 0.5;

        // Offset the heads from the base lines.
        private static double lineHeadExtension = 2.0;

        private static Material GridlineMaterialU = new Material("GridlineU", Colors.Red);
        private static Material GridlineMaterialV = new Material("GridlineV", new Color(0, 0.5, 0, 1));

        private static Material Grid2dElementMaterial = new Material("Grid2dElement", new Color(1, 1, 1, 0.01));

        /// <summary>
        ///
        /// </summary>
        /// <param name="model">The input model.</param>
        /// <param name="input">The arguments to the execution.</param>
        /// <returns>A GridOutputs instance containing computed results and the model with any new elements.</returns>
        public static GridOutputs Execute(Dictionary<string, Model> inputModels, GridInputs input)
        {
            var output = new GridOutputs();

            var extentPolygons = new List<Polygon>();

            inputModels.TryGetValue("Envelope", out var envelopeModel);
            if (envelopeModel != null)
            {
                Console.WriteLine("Got envelope model");
                var envelopes = envelopeModel.AllElementsOfType<Envelope>();
                extentPolygons = ExtractPolygonsFromElements(envelopes, (e) => e.Profile?.Perimeter?.TransformedPolygon(e.Transform));
            }

            inputModels.TryGetValue("Roof", out var roofModel);
            if (roofModel != null && extentPolygons.Count == 0)
            {
                var roofs = roofModel.AllElementsOfType<Roof>();
                extentPolygons = ExtractPolygonsFromElements(roofs, (e) => e.Profile?.Perimeter?.TransformedPolygon(e.Transform));
            }

            inputModels.TryGetValue("Conceptual Mass", out var conceptualMassModel);
            inputModels.TryGetValue("Levels", out var levelsModel);
            var levelVolumesModel = conceptualMassModel ?? levelsModel;
            if (levelVolumesModel != null && extentPolygons.Count == 0)
            {
                var levelVolumes = levelVolumesModel.AllElementsOfType<LevelVolume>();
                extentPolygons = ExtractPolygonsFromElements(levelVolumes, (e) => e.Profile?.Perimeter?.TransformedPolygon(e.Transform));
            }

            inputModels.TryGetValue("Floors", out var floorsModel);
            if (floorsModel != null && extentPolygons.Count == 0)
            {
                var floorVolumes = floorsModel.AllElementsOfType<Floor>();
                extentPolygons = ExtractPolygonsFromElements(floorVolumes, (e) => e.Profile?.Perimeter?.TransformedPolygon(e.Transform));
            }

            if (extentPolygons.Count == 0 && input.Mode == GridInputsMode.Typical)
            {
                // establish a default grid if there's nothing to base it on
                extentPolygons.Add(Polygon.Rectangle((0, 0), (40, 40)));
                output.Warnings.Add("Your model contains no Envelopes, Levels, or Floors from which to calculate extents — we've created a default grid for you.");
            }

            foreach (var gridArea in input.GridAreas)
            {
                output.Model.AddElements(CreateGridArea(input, output, extentPolygons, gridArea));
            }
            return output;
        }

        /// <summary>
        /// Extract polygons from a collection of elements.
        /// </summary>
        /// <param name="elements">The elements to get polygons from.</param>
        /// <param name="getDefaultPolygon">A function for extracting the default relevant polygon from an envelope.
        /// If this returns null, we'll use the 2D convex hull of the geometry of the element's representation </param>
        private static List<Polygon> ExtractPolygonsFromElements<T>(IEnumerable<T> elements, Func<T, Polygon> getDefaultPolygon) where T : GeometricElement
        {
            List<string> warnings = new List<string>();

            var polygons = new List<Polygon>();
            foreach (var element in elements)
            {
                var polygon = getDefaultPolygon(element) ??
                              ConvexHull.FromPoints(
                                  element?.Representation?.SolidOperations?
                                         .SelectMany(o => o?.Solid?.Vertices?
                                                          .Select(v => new Vector3(v.Value.Point.X, v.Value.Point.Y)) ?? Enumerable.Empty<Vector3>())
                                         .Where(v => v != null)
                              );
                if (polygon != null)
                {
                    polygons.Add(polygon);
                }
            }
            return polygons;
        }

        private static Transform GetOrigin(GridAreas gridArea, List<Polygon> envelopePolygons, GridInputs input)
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
            if (input.Overrides?.GridOrigins != null)
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
            GridInputs input,
            GridOutputs output,
            List<Polygon> envelopePolygons,
            GridAreas gridArea
        )
        {
            var transform = GetOrigin(gridArea, envelopePolygons, input);

            GridExtentsOverride extentsOverrideMatch = null;
            if (input.Overrides?.GridExtents != null)
            {
                extentsOverrideMatch = input.Overrides.GridExtents.FirstOrDefault(o => o.Identity.Name.Equals(gridArea.Name));
                if (extentsOverrideMatch != null)
                {
                    // TODO: multiple grid areas doesn't seem to be working right now, so we just assume a single grid area,
                    // and totally overwrite the grid polygons.
                    envelopePolygons = new List<Polygon>() { extentsOverrideMatch.Value.Extents };
                }
            }

            var origin = transform.Origin;
            var uDirection = transform.XAxis;
            var vDirection = transform.YAxis;

            if (input.ShowDebugGeometry)
            {
                // Draw origin
                output.Model.AddElement(new Line(origin - uDirection, origin + uDirection));
                output.Model.AddElement(new Line(origin - vDirection, origin + vDirection));
            }

            var uOverride = input.Overrides?.UGridSubdivisions?.FirstOrDefault(o => o.Identity.Name.Equals(gridArea.Name))?.Value.UGrid;
            var vOverride = input.Overrides?.VGridSubdivisions?.FirstOrDefault(o => o.Identity.Name.Equals(gridArea.Name))?.Value.VGrid;
            var standardizedRecords = GetStandardizedRecords(output, gridArea, input, envelopePolygons, transform, uOverride, vOverride);

            var u = standardizedRecords.u;
            var v = standardizedRecords.v;

            // CircleRadius = GetCircleRadius(u, v);

            var uDivisions = GetDivisions(origin, uDirection, u);
            var vDivisions = GetDivisions(origin, vDirection, v);

            var uPoints = uDivisions.Select(division => division.Point).ToList();
            var vPoints = vDivisions.Select(division => division.Point).ToList();

            var boundaries = new List<List<Polygon>>();
            var gridPoints = new List<Vector3>();

            foreach (var uPoint in uPoints)
            {
                foreach (var vPoint in vPoints)
                {
                    var offset = vPoint - origin;
                    var gridpoint = uPoint + offset;
                    gridPoints.Add(gridpoint);
                    if (input.ShowDebugGeometry)
                    {
                        output.Model.AddElement(new Panel(new Circle(0.25).ToPolygon(), null, new Transform(gridpoint)));
                    }
                }
            }

            if (gridPoints.Count == 0)
            {
                output.Errors.Add($"No grid points were able to be calculated from the given inputs for grid area {gridArea.Name}.");
                return new List<Grid2dElement>();
            }

            Polygon gridPolygon = null;

            try
            {
                gridPolygon = Polygon.FromAlignedBoundingBox2d(gridPoints);
            }
            catch
            {
                gridPolygon = Polygon.FromAlignedBoundingBox2d(new List<Vector3>() { origin, origin + uDirection, origin + vDirection });
            }

            if (input.ShowDebugGeometry)
            {
                output.Model.AddElement(new ModelCurve(gridPolygon));
            }

            var uDirectionEnd = origin + uDirection * (uOverride?.Curve.Length() ?? 1);
            var vDirectionEnd = origin + vDirection * (vOverride?.Curve.Length() ?? 1);
            var guideSegments = new List<Line>() { new Line(origin, uDirectionEnd), new Line(origin, vDirectionEnd) };
            if (envelopePolygons.Count() > 0)
            {
                var polygons = envelopePolygons.Concat(new List<Polygon>() { gridPolygon }).ToList();
                var unions = Polygon.UnionAll(polygons).ToList();
                var boundary = PolygonFromAlignedBoundingBox2d(
                    unions.Select(u => u.Vertices).SelectMany(x => x).Append(origin).Append(uDirectionEnd).Append(vDirectionEnd),
                    guideSegments);
                boundaries.Add(new List<Polygon>() { boundary });
            }
            else
            {
                // use points min and max
                var boundary = PolygonFromAlignedBoundingBox2d(
                    gridPolygon.Vertices.Append(origin).Append(uDirectionEnd).Append(vDirectionEnd),
                    guideSegments);
                boundaries.Add(new List<Polygon>() { boundary });
            }

            var texts = new List<(Vector3 location, Vector3 facingDirection, Vector3 lineDirection, string text, Color? color)>();
            var grid2dElements = new List<Grid2dElement>();
            foreach (var boundary in boundaries.SelectMany(boundaryList => boundaryList).ToList())
            {
                var gridNodes = new List<GridNode>();
                var grid = MakeGrid(boundary, origin, uDirection, vDirection, uPoints, vPoints);
                var uGridLines = CreateGridLines(input, output.Model, origin, uDivisions, grid.V, GridlineMaterialU, GridlineNamesIdentityAxis.U, input.BubbleRadius);
                var vGridLines = CreateGridLines(input, output.Model, origin, vDivisions, grid.U, GridlineMaterialV, GridlineNamesIdentityAxis.V, input.BubbleRadius);
                var allGridLines = uGridLines.Union(vGridLines);
                CheckDuplicatedNames(allGridLines, out var deduplicatedNamesGridLines);
                AddGridLinesTexts(allGridLines, deduplicatedNamesGridLines, texts);

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
                        var uCurve = uGridLine.Curve;
                        var vCurve = vGridLine.Curve;
                        if (Line.Intersects(uCurve.Start, uCurve.End, vCurve.Start, vCurve.End, out var intersection, includeEnds: true))
                        {
                            var gridNodeTransform = new Transform(intersection);
                            gridNodes.Add(new GridNode(gridNodeTransform,
                                                       uGridLine.Id.ToString(),
                                                       vGridLine.Id.ToString(),
                                                       Guid.NewGuid(),
                                                       $"{uGridLine.Name}{vGridLine.Name}"));

                            if (input.ShowDebugGeometry)
                            {
                                output.Model.AddElement(new Panel(new Circle(0.25).ToPolygon(), Debug.MagentaMaterial, new Transform(intersection)));
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

                var invert = new Transform(origin, uDirection, vDirection, Vector3.ZAxis);
                invert.Invert();
                var transformedBoundary = (Polygon)boundary.Transformed(invert);
                var rep = new Representation(new List<Elements.Geometry.Solids.SolidOperation>() { new Elements.Geometry.Solids.Lamina(transformedBoundary, false) });
                var grid2dElement = new Grid2dElement(grid, gridNodes,
                    uGridLines.Select(l => l.Id.ToString()).ToList(),
                    vGridLines.Select(l => l.Id.ToString()).ToList(),
                    transform, Grid2dElementMaterial, rep, false, Guid.NewGuid(), gridArea.Name);
                grid2dElement.Extents = boundary;
                grid2dElement.AdditionalProperties["UGrid"] = new Grid1dInput(
                    new Polyline(origin, grid.U.Curve.End),
                    uPoints,
                    uOverride?.SubdivisionMode ?? Grid1dInputSubdivisionMode.Manual,
                    uOverride?.SubdivisionSettings);
                grid2dElement.AdditionalProperties["VGrid"] = new Grid1dInput(
                    new Polyline(origin, grid.V.Curve.End),
                    vPoints,
                    vOverride?.SubdivisionMode ?? Grid1dInputSubdivisionMode.Manual,
                    vOverride?.SubdivisionSettings);
                if (extentsOverrideMatch != null)
                {
                    Identity.AddOverrideIdentity(grid2dElement, extentsOverrideMatch);
                }
                grid2dElements.Add(grid2dElement);
            }

            output.Model.AddElement(new ModelText(texts, GetFontSize(input.BubbleRadius, 50), 50));
            return grid2dElements;
        }

        private static FontSize GetFontSize(double radius, double scale)
        {
            double h = (2 * radius * pointsInMeter) / (Math.Sqrt(2) * scale);
            var fonts = Enum.GetValues(typeof(FontSize)).Cast<int>().ToList();

            for (int i = fonts.Count - 1; i >= 0; i--)
            {
                if (fonts[i] < h)
                {
                    return (FontSize)fonts[i];
                }
            }

            return (FontSize)fonts[0];
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
                name = ConvertToANamingPattern(idx);
            }
            if (namingPattern == "{1}")
            {
                name = (idx + 1).ToString();
            }
            return name;
        }

        // The pattern is the following: A-Z, AA-AZ, BA-BZ,..., AAA-AAZ,...
        private static string ConvertToANamingPattern(int idx)
        {
            // Let Q = 26 - number of uppercase characters, N - number of letters in string.
            // There are Q^N strings of length N. The first string of length N+1 will have
            // idx = Q^1 + Q^2 + ... + Q^n, which is equal to Q * (Q^N - 1) / (Q - 1).
            const int Q = 26;
            int N = (int)Math.Ceiling(Math.Log((Q - 1) * (idx + 1) + Q, Q) - 1);
            char[] chars = new char[N];

            // The goal is to represent idx in string S as a set of characters Ki, where 0 <= Ki < Q.
            // Such polynomial is unique for each idx.
            // S = K0 + K1 * Q + K2 * Q^2 + ... + K_n-1_ * Q^(N - 1).
            //
            // Each letter Ki are calculated in reverse order as reminder of dividing S by Q.
            // Then the letter is removed by dividing and thus dropping the reminder.
            for (int i = N - 1; i >= 0; i--)
            {
                //Uppercase A has code 65.
                chars[i] = (char)(idx % Q + 65);
                idx = (idx / Q) - 1;
            }

            return new string(chars);
        }

        private static void AddGridLinesTexts(
            IEnumerable<GridLine> gridlines,
            HashSet<GridLine> deduplicatedNamesGridLines,
            List<(Vector3 location, Vector3 facingDirection, Vector3 lineDirection, string text, Color? color)> texts)
        {
            foreach (var gridline in gridlines)
            {
                var curve = gridline.Curve;
                var lineDir = (curve.End - curve.Start).Unitized();
                var circleCenter = curve.Start - (lineDir * (gridline.Radius + lineHeadExtension));
                var color = deduplicatedNamesGridLines.Contains(gridline) ? Colors.Red : Colors.Darkgray;
                texts.Add((circleCenter, Vector3.ZAxis, lineDir, gridline.Name, color));
            }
        }

        private static List<GridGuide> GetDivisions(Vector3 origin, Vector3 gridDir, U u)
        {
            var gridGuides = new List<GridGuide>();
            var curPoint = origin;
            var sectionIdx = 0;
            var parametrizedPosition = 0.0;
            foreach (var gridline in u.GridLines)
            {
                curPoint = curPoint + gridDir * gridline.Spacing;
                parametrizedPosition += gridline.Spacing;
                gridGuides.Add(new GridGuide(curPoint, parametrizedPosition, GetName(u.Name, sectionIdx)));
                sectionIdx += 1;
            }
            return gridGuides;
        }

        private static void CheckDuplicatedNames(IEnumerable<GridLine> allGridLines, out HashSet<GridLine> deduplicatedNamesGridLines)
        {
            var gridLinesGroups = allGridLines.GroupBy(g => g.Name);
            deduplicatedNamesGridLines = new HashSet<GridLine>();
            foreach (var group in gridLinesGroups)
            {
                if (group.Count() > 1)
                {
                    var index = 1;
                    foreach (var gridLine in group.Skip(1))
                    {
                        gridLine.Name = $"{gridLine.Name}({index})";
                        deduplicatedNamesGridLines.Add(gridLine);
                        index++;
                    }
                }
            }
        }

        private static List<GridLine> CreateGridLines(GridInputs input,
                                                Model model,
                                                Vector3 origin,
                                                List<GridGuide> gridGuides,
                                                Grid1d opposingGrid1d,
                                                Material material,
                                                GridlineNamesIdentityAxis axis,
                                                double radius)
        {
            var baseLine = new Line(opposingGrid1d.Curve.Start, opposingGrid1d.Curve.End);

            var startExtend = origin - baseLine.Start;
            var endExtend = origin - baseLine.End;

            List<GridLine> gridLines = new List<GridLine>();
            foreach (var gridGuide in gridGuides)
            {
                var line = new Line(gridGuide.Point - startExtend, gridGuide.Point - endExtend);
                var gridline = new GridLine
                {
                    Radius = radius,
                    ExtensionBeginning = lineHeadExtension,
                    Curve = line,
                    Name = gridGuide.Name,
                    Material = material
                };
                gridline.AdditionalProperties[parametrizedPositionPropertyName] = gridGuide.ParametrizedPosition;
                gridline.AdditionalProperties[axisPropertyName] = axis;
                ApplyGridLineNameOverride(gridline, input, gridGuide.ParametrizedPosition, axis);
                model.AddElement(gridline);
                gridLines.Add(gridline);
            }

            return gridLines;
        }

        private static void ApplyGridLineNameOverride(
            GridLine gridLine,
            GridInputs input,
            double parametrizedPosition,
            GridlineNamesIdentityAxis axis)
        {
            var nameOverride = input.Overrides?.GridlineNames?
                               .FirstOrDefault(o => o.Identity.ParametrizedPosition.ApproximatelyEquals(parametrizedPosition)
                                                    && o.Identity.Axis.Equals(axis));
            if (nameOverride != null)
            {
                gridLine.Name = nameOverride.Value.Name;
                gridLine.AddOverrideIdentity("Gridline Names", nameOverride.Id, nameOverride.Identity);
            }
        }

        private static Polygon PolygonFromAlignedBoundingBox2d(IEnumerable<Vector3> points, List<Line> segments)
        {
            var minBoxArea = double.MaxValue;
            BBox3 minBox = new BBox3();
            Transform minBoxXform = new Transform();
            foreach (var edge in segments)
            {
                var edgeVector = edge.End - edge.Start;
                var xform = new Transform(Vector3.Origin, edgeVector, Vector3.ZAxis, 0);
                var invertedXform = new Transform(xform);
                invertedXform.Invert();
                var bbox = new BBox3(points.Select(p => invertedXform.OfPoint(p)).ToList());
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

        private static (U u, U v) GetStandardizedRecords(GridOutputs output, GridAreas gridArea, GridInputs input, List<Polygon> envelopePolygons, Transform transform, Grid1dInput uOverride, Grid1dInput vOverride)
        {
            var origin = transform.Origin;
            var uDirection = transform.XAxis;
            var vDirection = transform.YAxis;
            var u = gridArea.U;
            var v = gridArea.V;

            U uStandardized = null;
            U vStandardized = null;

            if (uOverride != null)
            {
                uStandardized = GetStandardizedRecords(u, uOverride);
            }

            if (vOverride != null)
            {
                var vAsU = new U(v.Name, v.Spacing, v.GridLines, v.TargetTypicalSpacing, v.OffsetStart, v.OffsetEnd);
                vStandardized = GetStandardizedRecords(vAsU, vOverride);
            }

            if (input.Mode == GridInputsMode.Typical)
            {
                var points = envelopePolygons.SelectMany(polygon => polygon.Vertices).Select(vertex => new Vector3(vertex.X, vertex.Y)).ToList();
                var bounds = GetBoundingBox2d(points, transform);

                if (origin.DistanceTo(bounds) > 0)
                {
                    points.Add(origin);
                    bounds = GetBoundingBox2d(points, transform);
                    output.Warnings.Add("Your origin is outside of your envelope boundaries. There are some assumptions made in this calculation that may not align to what you expect");
                }

                var xAxis = new Line(origin, origin + uDirection);
                xAxis = xAxis.ExtendTo(bounds, false, true);

                var yAxis = new Line(origin, origin + vDirection);
                yAxis = yAxis.ExtendTo(bounds, false, true);

                if (input.ShowDebugGeometry)
                {
                    output.Model.AddElement(new ModelCurve(bounds));
                }

                uStandardized ??= GetStandardizedRecords(u, input, xAxis.Length());
                vStandardized ??= GetStandardizedRecords(v, input, yAxis.Length());
            }
            else
            {
                uStandardized ??= GetStandardizedRecords(u, input, 0);
                vStandardized ??= GetStandardizedRecords(v, input, 0);
            }
            return (u: uStandardized, v: vStandardized);
        }

        private static U GetStandardizedRecords(U u, Grid1dInput uOverride)
        {
            var gridlines = new List<GridLines>();
            var start = uOverride.Curve.Start;
            var splitPoints = uOverride.SplitPoints
                                .Select(p => p.DistanceTo(start))
                                .OrderBy(d => d)
                                .ToList();
            for (var i = 0; i < splitPoints.Count; i++)
            {
                var prev = i == 0 ? 0 : splitPoints[i - 1];
                var spacing = splitPoints[i] - prev;
                gridlines.Add(new GridLines(0, spacing, 1));
            }
            return new U(u.Name, u.Spacing, gridlines, 0, 0, 0);
        }

        private static Polygon GetBoundingBox2d(IEnumerable<Vector3> points, Transform transform)
        {
            var hull = ConvexHull.FromPoints(points);
            var invertedXform = new Transform(transform);
            invertedXform.Invert();
            var transformedPolygon = hull.TransformedPolygon(invertedXform);
            var bbox = new BBox3(transformedPolygon.Vertices);
            var xy = new Plane(Vector3.Origin, Vector3.ZAxis);
            var boxRect = Polygon.Rectangle(bbox.Min.Project(xy), bbox.Max.Project(xy));
            return boxRect.TransformedPolygon(transform);
        }

        /// <summary>
        /// Standardizes records so that they all use relative spacing and not location,
        /// and have one gridline per instance (all quantities 1 : performs the duplication over quantity)
        /// </summary>
        /// <param name="u"></param>
        /// <param name="input"></param>
        /// <returns></returns>
        private static U GetStandardizedRecords(U u, GridInputs input, double axisLength)
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
            else if (input.Mode == GridInputsMode.Typical)
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
            else if (input.Mode == GridInputsMode.Absolute)
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

        private static U GetStandardizedRecords(V v, GridInputs input, double axisLength)
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

                var angle = axis.Direction().AngleTo(Vector3.XAxis);

                if (angle <= 45 && axes.x == null) // in case where both x and y will have the same angle of 45 degrees
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