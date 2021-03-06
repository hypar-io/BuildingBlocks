using System;
using System.Collections.Generic;
using System.Linq;
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
        private static List<Material> _lengthGradient = new List<Material>(){
            new Material(Colors.Green, 0.0f, 0.0f, false, null, false, Guid.NewGuid(), "Gradient 1"),
            new Material(Colors.Cyan, 0.0f, 0.0f, false, null, false, Guid.NewGuid(), "Gradient 2"),
            new Material(Colors.Lime, 0.0f, 0.0f, false, null, false, Guid.NewGuid(), "Gradient 3"),
            new Material(Colors.Yellow, 0.0f, 0.0f, false, null, false, Guid.NewGuid(), "Gradient 4"),
            new Material(Colors.Orange, 0.0f, 0.0f, false, null, false, Guid.NewGuid(), "Gradient 5"),
            new Material(Colors.Red, 0.0f, 0.0f, false, null, false, Guid.NewGuid(), "Gradient 6"),
        };

        private static double _longestGridSpan = 0.0;

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
            if (models.ContainsKey(GRIDS_MODEL_NAME))
            {
                var gridsModel = models[GRIDS_MODEL_NAME];
                var gridLines = gridsModel.AllElementsOfType<GridLine>();
                primaryDirection = gridLines.ElementAt(0).Geometry.Segments()[0].Direction();
            }
            else
            {
                warnings.Add("Adding the Grids function to your workflow will enable you to position and orient the grid. We'll use the default configuration for now with the grid oriented along the longest edge of the structure.");
                // Define the primary direction from the longest edge of the site.
                primaryDirection = longestEdge.Direction();
            }

            var structureMaterial = new Material("Steel", Colors.Gray, 0.5, 0.3);
            model.AddElement(structureMaterial);

            var wideFlangeFactory = new WideFlangeProfileFactory();
            var columnProfile = wideFlangeFactory.GetProfileByName(input.ColumnType.ToString());
            var colProfileBounds = columnProfile.Perimeter.Bounds();
            var colProfileDepth = colProfileBounds.Max.Y - colProfileBounds.Min.Y;
            var girderProfile = wideFlangeFactory.GetProfileByName(input.GirderType.ToString());
            var girdProfileBounds = girderProfile.Perimeter.Bounds();
            var girderProfileDepth = girdProfileBounds.Max.Y - girdProfileBounds.Min.Y;
            var beamProfile = wideFlangeFactory.GetProfileByName(input.BeamType.ToString());
            var beamProfileBounds = beamProfile.Perimeter.Bounds();
            var beamProfileDepth = beamProfileBounds.Max.Y - beamProfileBounds.Min.Y;

            var edges = cellComplex.GetEdges();
            var lowestTierSet = false;
            var lowestTierElevation = double.MaxValue;

            var columnDefintions = new Dictionary<(double memberLength, WideFlangeProfile memberProfile), Column>();
            var girderDefinitions = new Dictionary<(double memberLength, WideFlangeProfile memberProfile), Beam>();
            var beamDefinitions = new Dictionary<(double memberLength, WideFlangeProfile memberProfile), Beam>();

            // Order edges from lowest to highest.
            foreach (var edge in edges.OrderBy(e =>
                Math.Min(cellComplex.GetVertex(e.StartVertexId).Value.Z, cellComplex.GetVertex(e.EndVertexId).Value.Z)
            ))
            {
                var isExternal = edge.GetFaces().Count < 4;

                var start = cellComplex.GetVertex(edge.StartVertexId).Value;
                var end = cellComplex.GetVertex(edge.EndVertexId).Value;

                var l = new Line(start - new Vector3(0, 0, input.SlabThickness + girderProfileDepth / 2), end - new Vector3(0, 0, input.SlabThickness + girderProfileDepth / 2));
                var memberLength = l.Length();

                if (l.IsVertical())
                {
                    if (!input.InsertColumnsAtExternalEdges && isExternal)
                    {
                        continue;
                    }
                    var origin = start.IsLowerThan(end) ? start : end;
                    var rotation = Vector3.XAxis.AngleTo(primaryDirection);
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

                    Beam girderDefinition;
                    if (!girderDefinitions.ContainsKey((memberLength, girderProfile)))
                    {
                        // Beam definitions are defined along the Z axis
                        girderDefinition = new Beam(new Line(Vector3.Origin, new Vector3(0, 0, memberLength)), girderProfile, structureMaterial)
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

                    // Beam instances are transformed to align with the member's center line.
                    var t = new Transform(l.Start, l.Direction());
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
                var longestCellEdge = p.Segments().OrderBy(s => s.Length()).Last();
                var d = longestCellEdge.Direction();
                var beamGrid = new Grid1d(longestCellEdge);
                beamGrid.DivideByFixedLength(input.BeamSpacing, FixedDivisionMode.RemainderAtBothEnds);
                var segments = p.Segments();
                foreach (var pt in beamGrid.GetCellSeparators())
                {
                    // Skip beams that would be too close to the ends 
                    // to be useful.
                    if (pt.DistanceTo(longestCellEdge.Start) < 1 || pt.DistanceTo(longestCellEdge.End) < 1)
                    {
                        continue;
                    }
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
                            var l = new Line(t.Origin - new Vector3(0, 0, input.SlabThickness + beamProfileDepth / 2), xsect - new Vector3(0, 0, input.SlabThickness + beamProfileDepth / 2));
                            var beamLength = l.Length();
                            Beam beamDefinition;
                            if (!beamDefinitions.ContainsKey((beamLength, beamProfile)))
                            {
                                beamDefinition = new Beam(new Line(Vector3.Origin, new Vector3(0, 0, beamLength)), beamProfile, structureMaterial)
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
                            var instanceTransform = new Transform(l.Start, l.Direction());
                            var beamInstance = beamDefinition.CreateInstance(instanceTransform, $"beam_{cell.Id}");
                            beamInstance.AdditionalProperties.Add(CELL_ID_PROPERTY_NAME, cell.Id);
                            model.AddElement(beamInstance, false);
                        }
                    }
                }
            }

            var output = new StructureOutputs(_longestGridSpan);
            output.Model = model;
            output.Warnings = warnings;
            return output;
        }

        private static bool IsExternal(Edge e)
        {
            if (e.GetFaces().Count <= 3)
            {
                return true;
            }
            return false;
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