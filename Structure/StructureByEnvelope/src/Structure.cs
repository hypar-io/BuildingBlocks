using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Elements;
using Elements.Geometry;
using Elements.Geometry.Profiles;
using Elements.Geometry.Solids;
using Elements.Spatial;
using Elements.Spatial.CellComplex;
using System.Diagnostics;
using System.IO;

namespace Structure
{
    public static class Structure
    {
        private const string BAYS_MODEL_NAME = "Bays";
        private const string GRIDS_MODEL_NAME = "Grids";
        private const string LEVELS_MODEL_NAME = "Levels";
        private const string EDGE_ID_PROPERTY_NAME = "EdgeId";
        private const string EXTERNAL_EDGE_ID_PROPERTY_NAME = "ExternalEdgeId";
        private const string CELL_ID_PROPERTY_NAME = "CellId";
        private const double DEFAULT_U = 5.0;
        private const double DEFAULT_V = 7.0;
        private static readonly double _longestGridSpan = 0.0;

        /// <summary>
		/// The Structure function.
		/// </summary>
		/// <param name="model">The model.
		/// Add elements to the model to have them persisted.</param>
		/// <param name="input">The arguments to the execution.</param>
		/// <returns>A StructureOutputs instance containing computed results.</returns>
		public static StructureOutputs Execute(Dictionary<string, Model> models, StructureInputs input)
        {
            var output = new StructureOutputs();
            var model = new Model();
            var warnings = new List<string>();

            Elements.Validators.Validator.DisableValidationOnConstruction = true;

            CellComplex cellComplex = null;
            Line longestEdge = null;

#if DEBUG
            var sw = new Stopwatch();
            sw.Start();
#endif

            if (models.ContainsKey(BAYS_MODEL_NAME))
            {
                var cellsModel = models[BAYS_MODEL_NAME];
                var cellComplexes = cellsModel.AllElementsOfType<CellComplex>();

                if (cellComplexes.Count() == 0)
                {
                    output.Errors.Add($"No CellComplexes found in the model '{BAYS_MODEL_NAME}'. Check the output from the function upstream that has a model output '{BAYS_MODEL_NAME}'.");
                    return output;
                }

                cellComplex = cellComplexes.First();
            }
            else
            {
                warnings.Add("Adding the Bays function to your workflow will give you more configurability. We'll use the default configuration for now.");

                // Create a cell complex with some defaults.
                if (!models.ContainsKey(LEVELS_MODEL_NAME))
                {
                    output.Errors.Add("If Bays are not supplied then Levels are required.");
                    return output;
                }

                var levels = models[LEVELS_MODEL_NAME];
                var levelVolumes = levels.AllElementsOfType<LevelVolume>().ToList();
                if (levelVolumes.Count == 0)
                {
                    output.Errors.Add($"No LevelVolumes found in the model 'Levels'. Check the output from the function upstream that has a model output 'Levels'.");
                    return output;
                }

                // Replicate the old behavior by creating a
                // grid using the envelope's first level base polygon's longest
                // edge as the U axis and its perpendicular as the
                // V axis.

                var firstLevel = levelVolumes[0];
                var firstLevelPerimeter = firstLevel.Profile.Perimeter;
                longestEdge = firstLevelPerimeter.Segments().OrderBy(s => s.Length()).Last();

                var longestEdgeTransform = longestEdge.TransformAt(longestEdge.Domain.Mid());
                var t = new Transform(longestEdge.Start, longestEdgeTransform.XAxis, longestEdge.Direction(), Vector3.ZAxis);

                var toWorld = new Transform(t);
                toWorld.Invert();
                var bbox = new BBox3(firstLevelPerimeter.Vertices.Select(o => toWorld.OfVector(o)).ToList());

                var l = bbox.Max.Y - bbox.Min.Y;
                var w = bbox.Max.X - bbox.Min.X;

                var origin = t.OfVector(bbox.Min);

                var uGrid = new Grid1d(new Line(origin, origin + t.YAxis * l));
                uGrid.DivideByFixedLength(DEFAULT_U);

                var vGrid = new Grid1d(new Line(origin, origin + t.XAxis * w));

                vGrid.DivideByFixedLength(DEFAULT_V);
                var grid = new Grid2d(uGrid, vGrid);

                var u = grid.U;
                var v = grid.V;

                cellComplex = new CellComplex(Guid.NewGuid(), "Temporary Cell Complex");

                // Draw level volumes from each level down.
                for (var i = 1; i < levelVolumes.Count; i++)
                {
                    var levelVolume = levelVolumes.ElementAt(i);
                    var perimeter = levelVolume.Profile.Perimeter.Offset(-0.5)[0];
                    var g2d = new Grid2d(perimeter, grid.U, grid.V);
                    var levelElevation = levelVolume.Transform.Origin.Z;
                    var lastLevelVolume = levelVolumes.ElementAt(i - 1);
                    foreach (var cell in g2d.GetCells())
                    {
                        foreach (var crv in cell.GetTrimmedCellGeometry())
                        {
                            cellComplex.AddCell((Polygon)crv, lastLevelVolume.Height, levelElevation - lastLevelVolume.Height, g2d.U, g2d.V);
                            if (i == levelVolumes.Count - 1)
                            {
                                cellComplex.AddCell((Polygon)crv, levelVolume.Height, levelElevation, g2d.U, g2d.V);
                            }
                        }
                    }
                }
            }

#if DEBUG
            Console.WriteLine($"{sw.ElapsedMilliseconds} ms for getting or creating a cell complex.");
            sw.Restart();
#endif

            Vector3 primaryDirection;
            Vector3 secondaryDirection;
            IEnumerable<GridLine> gridLines = null;

            if (models.ContainsKey(GRIDS_MODEL_NAME))
            {
                var gridsModel = models[GRIDS_MODEL_NAME];
                gridLines = gridsModel.AllElementsOfType<GridLine>();

                // Group by direction.
                var gridGroups = gridLines.GroupBy(gl => gl.Curve.TransformAt(gl.Curve.Domain.Min).ZAxis).ToList();
                primaryDirection = gridGroups[0].Key;
                secondaryDirection = gridGroups[1].Key;
            }
            else
            {
                warnings.Add("Adding the Grids function to your workflow will enable you to position and orient the grid. We'll use the default configuration for now with the grid oriented along the longest edge of the structure.");
                // Define the primary direction from the longest edge of the site.
                primaryDirection = longestEdge.Direction();
                secondaryDirection = longestEdge.TransformAt(longestEdge.Domain.Mid()).XAxis;
            }

#if DEBUG
            Console.WriteLine($"{sw.ElapsedMilliseconds} ms for getting or creating grids.");
            sw.Restart();
#endif

            var structureMaterial = new Material("Steel", Colors.Gray, 0.5, 0.3);
            model.AddElement(structureMaterial, false);
            model.AddElement(BuiltInMaterials.ZAxis, false);

            var wideFlangeFactory = new WideFlangeProfileFactory();
            var shsProfileFactory = new SHSProfileFactory();
            var rhsProfileFactory = new RHSProfileFactory();

            var columnTypeName = input.ColumnType.ToString();
            var columnProfile = GetProfileFromName(columnTypeName, wideFlangeFactory, rhsProfileFactory, shsProfileFactory);
            var colProfileBounds = columnProfile.Perimeter.Bounds();
            var colProfileDepth = colProfileBounds.Max.Y - colProfileBounds.Min.Y;

            var girderTypeName = input.GirderType.ToString();
            Profile girderProfile = null;
            double girderProfileDepth = 0;
            if (girderTypeName.StartsWith("LH"))
            {
                girderProfileDepth = Units.InchesToMeters(double.Parse(girderTypeName.Split("LH")[1]));
            }
            else
            {
                girderProfile = GetProfileFromName(girderTypeName, wideFlangeFactory, rhsProfileFactory, shsProfileFactory);
                var girdProfileBounds = girderProfile.Perimeter.Bounds();
                girderProfileDepth = girdProfileBounds.Max.Y - girdProfileBounds.Min.Y;

                // Set the profile down by half its depth so that
                // it sits under the slab.
                girderProfile.Transform(new Transform(new Vector3(0, -girderProfileDepth / 2 - input.SlabThickness)));
            }

            Profile beamProfile = null;
            double beamProfileDepth = 0;

            var beamTypeName = input.BeamType.ToString();
            if (beamTypeName.StartsWith("LH"))
            {
                beamProfileDepth = Units.InchesToMeters(double.Parse(beamTypeName.Split("LH")[1]));
            }
            else
            {
                beamProfile = GetProfileFromName(beamTypeName, wideFlangeFactory, rhsProfileFactory, shsProfileFactory);
                var beamProfileBounds = beamProfile.Perimeter.Bounds();
                beamProfileDepth = beamProfileBounds.Max.Y - beamProfileBounds.Min.Y;

                // Set the profile down by half its depth so that
                // it sits under the slab.
                beamProfile.Transform(new Transform(new Vector3(0, -beamProfileDepth / 2 - input.SlabThickness)));
            }

            var edges = cellComplex.GetEdges();
            var lowestTierSet = false;
            var lowestTierElevation = double.MaxValue;

            var columnDefintions = new Dictionary<(double memberLength, Profile memberProfile), Column>();
            var girderDefinitions = new Dictionary<(double memberLength, Profile memberProfile), GeometricElement>();
            var beamDefinitions = new Dictionary<(double memberLength, Profile memberProfile), GeometricElement>();
            var girderJoistDefinitions = new Dictionary<(double memberLength, double depth), GeometricElement>();
            var beamJoistDefinitions = new Dictionary<(double memberLength, double depth), GeometricElement>();

            LProfileFactory lProfileFactory;
            LProfile L8 = null;
            LProfile L5 = null;
            LProfile L2 = null;
            LProfile L3 = null;

            if (girderProfile == null || beamProfile == null)
            {
                lProfileFactory = new LProfileFactory();
                L8 = Task.Run(async () => await lProfileFactory.GetProfileByTypeAsync(LProfileType.L8X8X1_2)).Result;
                L5 = Task.Run(async () => await lProfileFactory.GetProfileByTypeAsync(LProfileType.L5X5X1_2)).Result;
                L2 = Task.Run(async () => await lProfileFactory.GetProfileByTypeAsync(LProfileType.L2X2X1_8)).Result;
                L3 = Task.Run(async () => await lProfileFactory.GetProfileByTypeAsync(LProfileType.L3X2X3_16)).Result;
            }

#if DEBUG
            Console.WriteLine($"{sw.ElapsedMilliseconds} ms for getting all beam and column profiles.");
            sw.Restart();
#endif

            var xy = new Plane(Vector3.Origin, Vector3.ZAxis);

            // Order edges from lowest to highest.
            foreach (Elements.Spatial.CellComplex.Edge edge in edges.OrderBy(e =>
                Math.Min(cellComplex.GetVertex(e.StartVertexId).Value.Z, cellComplex.GetVertex(e.EndVertexId).Value.Z)
            ))
            {
                var memberLength = edge.Length(cellComplex);
                var start = cellComplex.GetVertex(edge.StartVertexId).Value;
                var end = cellComplex.GetVertex(edge.EndVertexId).Value;
                var direction = (end - start).Unitized();

                var warningRepresentation = new Representation(new List<SolidOperation>() { new Extrude(Polygon.Rectangle(0.01, 0.01), 0.01, Vector3.ZAxis, false) });

                if (edge.IsVertical(cellComplex))
                {
                    // For vertical edges that are not on the grid, we need
                    // a heuristic to determine when we should place a column.
                    // You don't want to place a column all the time because
                    // for non-grid-aligned structures, when you place columns
                    // at every intersection of the envelope and the grid, you
                    // can get columns that are too close together. Instead, we
                    // place a column based on the distance from that column along
                    // a grid line back to a primary grid intersection. If that
                    // distance exceeds the maximum allowable neighbor span,
                    // we place a column.
                    if (!edge.StartsOnGrid(cellComplex))
                    {
                        var maxDistance = double.MinValue;
                        foreach (var e in edge.GetCells().SelectMany(c => c.GetEdges().Where(e =>
                                                e != edge &&
                                                e.StartsOrEndsOnGrid(cellComplex) &&
                                                e.StartsOrEndsAtThisVertex(edge.StartVertexId, cellComplex) &&
                                                e.IsHorizontal(cellComplex))))
                        {
                            var d = e.Length(cellComplex);
                            maxDistance = Math.Max(maxDistance, d);
                        }

                        if (maxDistance < input.MaximumNeighborSpan)
                        {
                            continue;
                        }
                    }

                    var origin = start.IsLowerThan(end) ? start : end;
                    var rotation = Vector3.XAxis.PlaneAngleTo(primaryDirection);
                    Column columnDefinition;
                    if (!columnDefintions.ContainsKey((memberLength, columnProfile)))
                    {
                        columnDefinition = new Column(Vector3.Origin, memberLength, null, columnProfile, material: structureMaterial, name: columnProfile.Name)
                        {
                            IsElementDefinition = true
                        };
                        columnDefinition.Representation.SkipCSGUnion = true;
                        columnDefintions.Add((memberLength, columnProfile), columnDefinition);
                        model.AddElement(columnDefinition, false);
                    }
                    else
                    {
                        columnDefinition = columnDefintions[(memberLength, columnProfile)];
                    }
                    var t = new Transform();
                    t.Rotate(rotation);
                    t.Move(origin);
                    var instance = columnDefinition.CreateInstance(t, $"{columnDefinition.Name}");
                    instance.AdditionalProperties.Add(EDGE_ID_PROPERTY_NAME, edge.Id);
                    model.AddElement(instance, false);
                    model.AddElement(new ModelCurve(new Line(columnDefinition.Location, columnDefinition.Location + new Vector3(0, 0, columnDefinition.Height)).TransformedLine(t), BuiltInMaterials.ZAxis), false);
                }
                else
                {
                    if (!lowestTierSet)
                    {
                        lowestTierElevation = start.Z;
                        lowestTierSet = true;
                    }

                    GeometricElement girderDefinition;
                    if (girderProfile != null)
                    {
                        FindOrCreateStructuralFramingDefinition(memberLength, girderProfile, structureMaterial, girderDefinitions, model, out girderDefinition);
                    }
                    else
                    {
                        // Create a joist
                        if (!girderJoistDefinitions.ContainsKey((memberLength, girderProfileDepth)))
                        {
                            // Beam definitions are defined along the X axis
                            var cl = new Line(Vector3.Origin, new Vector3(memberLength, 0));
                            if (memberLength < 1.0)
                            {
                                continue;
                            }

                            var cellCount = (int)Math.Ceiling((memberLength - Units.InchesToMeters(24)) / girderProfileDepth);
                            girderDefinition = new Joist(cl,
                                                         L8,
                                                         L8,
                                                         L2,
                                                         girderProfileDepth,
                                                         cellCount,
                                                         Units.InchesToMeters(2.5),
                                                         Units.InchesToMeters(12),
                                                         structureMaterial,
                                                         L8.Name)
                            {
                                IsElementDefinition = true
                            };
                            girderDefinition.Representation.SkipCSGUnion = true;
                            girderJoistDefinitions.Add((memberLength, girderProfileDepth), girderDefinition);
                            model.AddElement(girderDefinition, false);
                        }
                        else
                        {
                            girderDefinition = girderJoistDefinitions[(memberLength, girderProfileDepth)];
                        }
                    }

                    // Beam instances are transformed to align with the member's center line.
                    var t = new Transform(start, direction, Vector3.ZAxis);
                    ElementInstance girderInstance = null;
                    if (input.CreateBeamsOnFirstLevel)
                    {
                        girderInstance = girderDefinition.CreateInstance(t, $"{girderDefinition.Name}");
                        model.AddElement(girderInstance, false);

                        var modelCurve = CreateModelCurve(girderDefinition, t);
                        if (modelCurve != null)
                        {
                            model.AddElement(modelCurve, false);
                        }
                    }
                    else
                    {
                        if (start.Z > lowestTierElevation)
                        {
                            girderInstance = girderDefinition.CreateInstance(t, $"{girderDefinition.Name}");
                            model.AddElement(girderInstance, false);

                            var modelCurve = CreateModelCurve(girderDefinition, t);
                            if (modelCurve != null)
                            {
                                model.AddElement(modelCurve, false);
                            }
                        }
                    }

                    if (girderInstance != null)
                    {
                        girderInstance.AdditionalProperties.Add(EDGE_ID_PROPERTY_NAME, edge.Id);
                        if (IsExternal(edge))
                        {
                            girderInstance.AdditionalProperties.Add(EXTERNAL_EDGE_ID_PROPERTY_NAME, edge.Id);
                        }
                    }
                }
            }

#if DEBUG
            Console.WriteLine($"{sw.ElapsedMilliseconds} ms for creating girders and columns.");
            sw.Restart();
#endif

            foreach (var cell in cellComplex.GetCells())
            {
                var topFace = cell.GetTopFace();
                var p = topFace.GetGeometry();
                var segments = p.Segments();

                // Get the longest cell edge that is parallel
                // to one of the primary directions.
                var cellEdges = segments.Where(s =>
                {
                    var d = s.Direction();
                    return d.IsParallelTo(primaryDirection) || d.IsParallelTo(secondaryDirection);
                });

                Line longestCellEdge;
                if (cellEdges.Any())
                {
                    longestCellEdge = cellEdges.OrderBy(s => s.Length()).Last();
                }
                else
                {
                    longestCellEdge = segments.OrderBy(s => s.Length()).Last();
                }

                var d = longestCellEdge.Direction();
                var length = longestCellEdge.Length();
                var remainder = length % input.BeamSpacing;

                var beamGrid = new Grid1d(longestCellEdge);
                beamGrid.DivideByApproximateLength(input.BeamSpacing, EvenDivisionMode.RoundDown);

                var cellSeparators = beamGrid.GetCellSeparators();

                for (var i = 0; i < cellSeparators.Count; i++)
                {
                    if (i == 0 || i == cellSeparators.Count - 1)
                    {
                        continue;
                    }

                    var pt = cellSeparators[i];
                    var t = new Transform(pt, d, Vector3.ZAxis);
                    var r = new Ray(t.Origin, t.YAxis);
                    foreach (var s in segments)
                    {
                        if (s == longestCellEdge)
                        {
                            continue;
                        }

                        if (r.Intersects(s, out Vector3 xsect))
                        {
                            if (t.Origin.DistanceTo(xsect) < 1)
                            {
                                continue;
                            }

                            var l = new Line(t.Origin, xsect);
                            var beamLength = l.Length();

                            GeometricElement beamDefinition;
                            if (beamProfile != null)
                            {
                                FindOrCreateStructuralFramingDefinition(beamLength, beamProfile, structureMaterial, beamDefinitions, model, out beamDefinition);
                            }
                            else
                            {
                                // Create a joist
                                if (!beamJoistDefinitions.ContainsKey((beamLength, beamProfileDepth)))
                                {
                                    // Beam definitions are defined along the X axis
                                    var cl = new Line(Vector3.Origin, new Vector3(beamLength, 0));
                                    // if (beamLength < beamProfileDepth)
                                    // {
                                    //     continue;
                                    // }

                                    var cellCount = (int)Math.Ceiling((beamLength - Units.InchesToMeters(24)) / beamProfileDepth);
                                    beamDefinition = new Joist(cl, L3, L3, L2, beamProfileDepth, cellCount, Units.InchesToMeters(2.5), Units.InchesToMeters(12), structureMaterial)
                                    {
                                        IsElementDefinition = true
                                    };
                                    beamDefinition.Representation.SkipCSGUnion = true;
                                    beamJoistDefinitions.Add((beamLength, beamProfileDepth), beamDefinition);
                                    model.AddElement(beamDefinition, false);
                                }
                                else
                                {
                                    beamDefinition = beamJoistDefinitions[(beamLength, beamProfileDepth)];
                                }
                            }
                            var beamDir = l.Direction();
                            var instanceTransform = new Transform(l.Start, beamDir, Vector3.ZAxis);
                            var beamInstance = beamDefinition.CreateInstance(instanceTransform, $"{beamDefinition.Name}");
                            beamInstance.AdditionalProperties.Add(CELL_ID_PROPERTY_NAME, cell.Id);
                            model.AddElement(beamInstance, false);
                            var planDirection = beamDir.IsAlmostEqualTo(Vector3.ZAxis) ? Vector3.XAxis : beamDir.Project(xy).Unitized();
                            beamInstance.AdditionalProperties.Add("LabelConfiguration", new LabelConfiguration(new Color(1, 1, 1, 0), Vector3.Origin, null, null, planDirection));
                            var modelCurve = CreateModelCurve(beamDefinition, instanceTransform);
                            if (modelCurve != null)
                            {
                                model.AddElement(modelCurve, false);
                            }
                        }
                    }
                }
            }

#if DEBUG
            Console.WriteLine($"{sw.ElapsedMilliseconds} ms for creating beams.");
            sw.Restart();
#endif

            model.AddElements(CreateViewScopesForLevelsAndGrids(model, gridLines), false);

            output.MaximumBeamLength = _longestGridSpan;
            output.Model = model;
            output.Warnings = warnings;
            return output;
        }

        private static ModelCurve CreateModelCurve(GeometricElement sf, Transform t)
        {
            BoundedCurve curve = null;
            if (sf is Beam beam)
            {
                curve = beam.Curve.Transformed(t) as BoundedCurve;
            }
            else if (sf is Joist joist)
            {
                curve = joist.Curve.Transformed(t) as BoundedCurve;
            }

            return curve == null ? null : new ModelCurve(curve, BuiltInMaterials.ZAxis);
        }

        private static void FindOrCreateStructuralFramingDefinition(double memberLength,
                                                             Profile framingProfile,
                                                             Material material,
                                                             Dictionary<(double, Profile), GeometricElement> structuralFramingDefinitions,
                                                             Model model,
                                                             out GeometricElement structuralFramingDefinition)
        {
            if (!structuralFramingDefinitions.ContainsKey((memberLength, framingProfile)))
            {
                // Beam definitions are defined along the X axis
                var cl = new Line(Vector3.Origin, new Vector3(memberLength, 0));
                structuralFramingDefinition = new Beam(cl, framingProfile, material: material, name: framingProfile.Name)
                {
                    IsElementDefinition = true
                };
                structuralFramingDefinition.Representation.SkipCSGUnion = true;
                structuralFramingDefinitions.Add((memberLength, framingProfile), structuralFramingDefinition);
                model.AddElement(structuralFramingDefinition, false);
            }
            else
            {
                structuralFramingDefinition = structuralFramingDefinitions[(memberLength, framingProfile)];
            }
        }

        private static List<ViewScope> CreateViewScopesForLevelsAndGrids(Model model, IEnumerable<GridLine> gridLines)
        {
            var beams = model.AllElementsOfType<ElementInstance>().Where(e => e.BaseDefinition is Beam || e.BaseDefinition is Joist);
            var beamGroups = beams.GroupBy(b => b.Transform.Origin.Z);

            var scopes = new List<ViewScope>();
            var minZ = double.MaxValue;
            var maxZ = double.MinValue;

            foreach (var bg in beamGroups)
            {
                var bbox = new BBox3(bg.SelectMany(b =>
                {
                    var def = (StructuralFraming)b.BaseDefinition;
                    var start = b.Transform.OfPoint(def.Curve.Start);
                    var end = b.Transform.OfPoint(def.Curve.End);

                    if (start.Z > maxZ)
                    {
                        maxZ = start.Z;
                    }
                    if (start.Z < minZ)
                    {
                        minZ = start.Z;
                    }
                    if (end.Z > maxZ)
                    {
                        maxZ = end.Z;
                    }
                    if (end.Z < minZ)
                    {
                        minZ = end.Z;
                    }
                    return new[] { start, end };
                }));

                var scope = new ViewScope()
                {
                    BoundingBox = new BBox3(bbox.Min + new Vector3(0, 0, -1), bbox.Max + new Vector3(0, 0, 1)),
                    Name = $"Structure elevation {bg.Key}",
                    Camera = new Camera(new Vector3(0, 0, -1), null, CameraProjection.Orthographic)
                };
                scopes.Add(scope);
            }

            // TODO: Create view scopes along grid lines when view scopes
            // support non-axis aligned bounding boxes.
            // if (gridLines != null)
            // {
            //     foreach (var gridLine in gridLines)
            //     {

            //         var d = gridLine.Line.Direction();
            //         var ortho = Vector3.ZAxis.Cross(gridLine.Line.Direction());
            //         var bbox = new BBox3(gridLine);
            //         var scope = new ViewScope()
            //         {
            //             BoundingBox = new BBox3(bbox.Min + ortho * -1, bbox.Max + ortho * 1),
            //             Name = $"Structure Grid {gridLine.Name}",
            //             Camera = new Camera(ortho, null, CameraProjection.Perspective)
            //         };
            //         scopes.Add(scope);
            //     }
            // }
            return scopes;
        }
        private static bool IsExternal(Elements.Spatial.CellComplex.Edge e)
        {
            var faces = e.GetFaces();
            if (faces.Count <= 3)
            {
                return true;
            }
            return false;
        }
        private static Profile GetProfileFromName(string name, WideFlangeProfileFactory wideFlangeFactory, RHSProfileFactory rhsFactory, SHSProfileFactory shsFactory)
        {
            Profile profile = wideFlangeFactory.GetProfileByType(WideFlangeProfileType.W4x13);
            if (name.StartsWith("W"))
            {
                profile = wideFlangeFactory.GetProfileByName(name);
            }
            else if (name.StartsWith("RHS"))
            {
                profile = rhsFactory.GetProfileByName(name);
            }
            else if (name.StartsWith("SHS"))
            {
                profile = shsFactory.GetProfileByName(name);
            }
            return profile;
        }
    }
}

internal static class Vector3Extensions
{
    public static bool IsDirectlyUnder(this Vector3 a, Vector3 b)
    {
        return a.Z > b.Z && a.X.ApproximatelyEquals(b.X) && a.Y.ApproximatelyEquals(b.Y);
    }

    public static bool IsHigherThan(this Vector3 a, Vector3 b)
    {
        return a.Z > b.Z;
    }

    public static bool IsLowerThan(this Vector3 a, Vector3 b)
    {
        return a.Z < b.Z;
    }

    public static bool IsVertical(this Line line)
    {
        return line.Start.IsDirectlyUnder(line.End) || line.End.IsDirectlyUnder(line.Start);
    }

    public static bool IsHorizontal(this Elements.Spatial.CellComplex.Edge edge, CellComplex cellComplex)
    {
        return cellComplex.GetVertex(edge.StartVertexId).Value.Z.ApproximatelyEquals(cellComplex.GetVertex(edge.EndVertexId).Value.Z);
    }

    public static bool IsVertical(this Elements.Spatial.CellComplex.Edge edge, CellComplex cellComplex)
    {
        return cellComplex.GetVertex(edge.StartVertexId).Value.IsDirectlyUnder(cellComplex.GetVertex(edge.EndVertexId).Value) ||
            cellComplex.GetVertex(edge.EndVertexId).Value.IsDirectlyUnder(cellComplex.GetVertex(edge.StartVertexId).Value);
    }

    public static double Length(this Elements.Spatial.CellComplex.Edge edge, CellComplex cellComplex)
    {
        return cellComplex.GetVertex(edge.StartVertexId).Value.DistanceTo(cellComplex.GetVertex(edge.EndVertexId).Value);
    }

    public static Line ToLine(this Elements.Spatial.CellComplex.Edge edge, CellComplex cellComplex)
    {
        var start = cellComplex.GetVertex(edge.StartVertexId).Value;
        var end = cellComplex.GetVertex(edge.EndVertexId).Value;
        var l = new Line(start, end);
        return l;
    }

    public static bool StartsOnGrid(this Elements.Spatial.CellComplex.Edge edge, CellComplex cellComplex)
    {
        return !string.IsNullOrEmpty(cellComplex.GetVertex(edge.StartVertexId).Name);
    }

    public static bool EndsOnGrid(this Elements.Spatial.CellComplex.Edge edge, CellComplex cellComplex)
    {
        return !string.IsNullOrEmpty(cellComplex.GetVertex(edge.EndVertexId).Name);
    }

    public static bool StartsOrEndsOnGrid(this Elements.Spatial.CellComplex.Edge edge, CellComplex cellComplex)
    {
        return StartsOnGrid(edge, cellComplex) || EndsOnGrid(edge, cellComplex);
    }

    public static bool StartsOrEndsAtThisVertex(this Elements.Spatial.CellComplex.Edge edge, ulong vertexId, CellComplex cellComplex)
    {
        return edge.StartVertexId == vertexId || edge.EndVertexId == vertexId;
    }
}