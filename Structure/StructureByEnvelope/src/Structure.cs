using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Elements;
using Elements.Geometry;
using Elements.Geometry.Profiles;
using Elements.Spatial;
using Elements.Spatial.CellComplex;

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
            var model = new Model();
            var warnings = new List<string>();

            CellComplex cellComplex = null;
            Line longestEdge = null;

            if (models.ContainsKey(BAYS_MODEL_NAME))
            {
                var cellsModel = models[BAYS_MODEL_NAME];
                cellComplex = cellsModel.AllElementsOfType<CellComplex>().First();
            }
            else
            {
                warnings.Add("Adding the Bays function to your workflow will give you more configurability. We'll use the default configuration for now.");

                // Create a cell complex with some defaults.
                if (!models.ContainsKey(LEVELS_MODEL_NAME))
                {
                    throw new Exception("If Bays are not supplied Levels are required.");
                }

                var levels = models[LEVELS_MODEL_NAME];
                var levelVolumes = levels.AllElementsOfType<LevelVolume>().ToList();
                if (levelVolumes.Count == 0)
                {
                    throw new Exception("No LevelVolumes found in your Levels model. Please use a level function that generates LevelVolumes, such as Simple Levels by Envelope");
                }

                // Replicate the old behavior by creating a 
                // grid using the envelope's first level base polygon's longest
                // edge as the U axis and its perpendicular as the
                // V axis.

                var firstLevel = levelVolumes[0];
                var firstLevelPerimeter = firstLevel.Profile.Perimeter;
                longestEdge = firstLevelPerimeter.Segments().OrderBy(s => s.Length()).Last();

                var longestEdgeTransform = longestEdge.TransformAt(0.5);
                var t = new Transform(longestEdge.Start, longestEdgeTransform.XAxis, longestEdge.Direction(), Vector3.ZAxis);

                var toWorld = new Transform(t);
                toWorld.Invert();
                var bbox = new BBox3(firstLevelPerimeter.Vertices.Select(o => toWorld.OfVector(o)).ToList());

                model.AddElements(new ModelCurve(Polygon.Rectangle(bbox.Min, bbox.Max).Transformed(t)));

                // model.AddElements(toWorld.ToModelCurves());

                var l = bbox.Max.Y - bbox.Min.Y;
                var w = bbox.Max.X - bbox.Min.X;

                var origin = t.OfVector(bbox.Min);

                var uGrid = new Grid1d(new Line(origin, origin + t.YAxis * l));
                uGrid.DivideByFixedLength(DEFAULT_U);

                var vGrid = new Grid1d(new Line(origin, origin + t.XAxis * w));

                vGrid.DivideByFixedLength(DEFAULT_V);
                var grid = new Grid2d(uGrid, vGrid);

                // model.AddElements(grid.ToModelCurves());

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

            Vector3 primaryDirection;
            Vector3 secondaryDirection;
            if (models.ContainsKey(GRIDS_MODEL_NAME))
            {
                var gridsModel = models[GRIDS_MODEL_NAME];
                var gridLines = gridsModel.AllElementsOfType<GridLine>();

                // Group by direction.
                var gridGroups = gridLines.GroupBy(gl => gl.Line.Direction()).ToList();
                primaryDirection = gridGroups[0].Key;
                secondaryDirection = gridGroups[1].Key;
            }
            else
            {
                warnings.Add("Adding the Grids function to your workflow will enable you to position and orient the grid. We'll use the default configuration for now with the grid oriented along the longest edge of the structure.");
                // Define the primary direction from the longest edge of the site.
                primaryDirection = longestEdge.Direction();
                secondaryDirection = longestEdge.TransformAt(0.5).XAxis;
            }

            var structureMaterial = new Material("Steel", Colors.Gray, 0.5, 0.3);
            model.AddElement(structureMaterial);

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
                girderProfile = girderProfile.Transformed(new Transform(new Vector3(0, -girderProfileDepth / 2 - input.SlabThickness)));
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
                beamProfile = beamProfile.Transformed(new Transform(new Vector3(0, -beamProfileDepth / 2 - input.SlabThickness)));
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

            // Order edges from lowest to highest.
            foreach (var edge in edges.OrderBy(e =>
                Math.Min(cellComplex.GetVertex(e.StartVertexId).Value.Z, cellComplex.GetVertex(e.EndVertexId).Value.Z)
            ))
            {
                var isExternal = edge.GetFaces().Count < 4;

                var start = cellComplex.GetVertex(edge.StartVertexId).Value;
                var end = cellComplex.GetVertex(edge.EndVertexId).Value;

                var l = new Line(start, end);
                var memberLength = l.Length();

                if (l.IsVertical())
                {
                    if (!input.InsertColumnsAtExternalEdges && isExternal)
                    {
                        continue;
                    }
                    var origin = start.IsLowerThan(end) ? start : end;
                    var rotation = Vector3.XAxis.PlaneAngleTo(primaryDirection);
                    Column columnDefinition;
                    if (!columnDefintions.ContainsKey((memberLength, columnProfile)))
                    {
                        columnDefinition = new Column(Vector3.Origin, memberLength, columnProfile, structureMaterial)
                        {
                            IsElementDefinition = true
                        };
                        columnDefintions.Add((memberLength, columnProfile), columnDefinition);
                        model.AddElement(columnDefinition);
                    }
                    else
                    {
                        columnDefinition = columnDefintions[(memberLength, columnProfile)];
                    }
                    var t = new Transform();
                    t.Rotate(rotation);
                    t.Move(origin);
                    var instance = columnDefinition.CreateInstance(t, $"column_{edge.Id}");
                    instance.AdditionalProperties.Add(EDGE_ID_PROPERTY_NAME, edge.Id);
                    model.AddElement(instance, false);
                }
                else
                {
                    if (!lowestTierSet)
                    {
                        lowestTierElevation = l.Start.Z;
                        lowestTierSet = true;
                    }

                    GeometricElement girderDefinition;
                    if (girderProfile != null)
                    {
                        if (!girderDefinitions.ContainsKey((memberLength, girderProfile)))
                        {
                            // Beam definitions are defined along the X axis
                            var cl = new Line(Vector3.Origin, new Vector3(memberLength, 0));
                            girderDefinition = new Beam(cl, girderProfile, structureMaterial)
                            {
                                IsElementDefinition = true
                            };
                            girderDefinitions.Add((memberLength, girderProfile), girderDefinition);
                            model.AddElement(girderDefinition);
                        }
                        else
                        {
                            girderDefinition = girderDefinitions[(memberLength, girderProfile)];
                        }
                    }
                    else
                    {
                        // Create a joist
                        if (!girderJoistDefinitions.ContainsKey((memberLength, girderProfileDepth)))
                        {
                            // Beam definitions are defined along the X axis
                            var cl = new Line(Vector3.Origin, new Vector3(memberLength, 0));
                            if (memberLength < girderProfileDepth)
                            {
                                continue;
                            }

                            var cellCount = (int)Math.Ceiling((memberLength - Units.InchesToMeters(24)) / girderProfileDepth);
                            girderDefinition = new Joist(cl, L8, L8, L2, girderProfileDepth, cellCount, Units.InchesToMeters(2.5), Units.InchesToMeters(12), structureMaterial)
                            {
                                IsElementDefinition = true
                            };
                            girderJoistDefinitions.Add((memberLength, girderProfileDepth), girderDefinition);
                            model.AddElement(girderDefinition);
                        }
                        else
                        {
                            girderDefinition = girderJoistDefinitions[(memberLength, girderProfileDepth)];
                        }
                    }

                    // Beam instances are transformed to align with the member's center line.
                    var t = new Transform(l.Start, l.Direction(), Vector3.ZAxis);
                    ElementInstance girderInstance = null;
                    if (input.CreateBeamsOnFirstLevel)
                    {
                        girderInstance = girderDefinition.CreateInstance(t, $"beam_{edge.Id}");
                        model.AddElement(girderInstance, false);
                    }
                    else
                    {
                        if (l.Start.Z > lowestTierElevation)
                        {
                            girderInstance = girderDefinition.CreateInstance(t, $"beam_{edge.Id}");
                            model.AddElement(girderInstance, false);
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
                                if (!beamDefinitions.ContainsKey((beamLength, beamProfile)))
                                {
                                    beamDefinition = new Beam(new Line(Vector3.Origin, new Vector3(beamLength, 0)), beamProfile, structureMaterial)
                                    {
                                        IsElementDefinition = true
                                    };
                                    beamDefinitions.Add((beamLength, beamProfile), beamDefinition);
                                    model.AddElement(beamDefinition);
                                }
                                else
                                {
                                    beamDefinition = beamDefinitions[(beamLength, beamProfile)];
                                }
                            }
                            else
                            {
                                // Create a joist
                                if (!beamJoistDefinitions.ContainsKey((beamLength, beamProfileDepth)))
                                {
                                    // Beam definitions are defined along the X axis
                                    var cl = new Line(Vector3.Origin, new Vector3(beamLength, 0));
                                    if (beamLength < beamProfileDepth)
                                    {
                                        continue;
                                    }

                                    var cellCount = (int)Math.Ceiling((beamLength - Units.InchesToMeters(24)) / beamProfileDepth);
                                    beamDefinition = new Joist(cl, L3, L3, L2, beamProfileDepth, cellCount, Units.InchesToMeters(2.5), Units.InchesToMeters(12), structureMaterial)
                                    {
                                        IsElementDefinition = true
                                    };
                                    beamJoistDefinitions.Add((beamLength, beamProfileDepth), beamDefinition);
                                    model.AddElement(beamDefinition);
                                }
                                else
                                {
                                    beamDefinition = beamJoistDefinitions[(beamLength, beamProfileDepth)];
                                }
                            }
                            var instanceTransform = new Transform(l.Start, l.Direction(), Vector3.ZAxis);
                            var beamInstance = beamDefinition.CreateInstance(instanceTransform, $"beam_{cell.Id}");
                            beamInstance.AdditionalProperties.Add(CELL_ID_PROPERTY_NAME, cell.Id);
                            model.AddElement(beamInstance, false);
                        }
                    }
                }
            }

            var output = new StructureOutputs(_longestGridSpan)
            {
                Model = model,
                Warnings = warnings
            };
            return output;
        }

        private static bool IsExternal(Edge e)
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
}