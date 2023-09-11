using Elements;
using Elements.Geometry;
using Elements.Spatial;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FacadeGridByLevels
{
    public static class FacadeGridByLevels
    {
        /// <summary>
        /// The FacadeGridByLevels function.
        /// </summary>
        /// <param name="model">The input model.</param>
        /// <param name="input">The arguments to the execution.</param>
        /// <returns>A FacadeGridByLevelsOutputs instance containing computed results and the model with any new elements.</returns>
        public static FacadeGridByLevelsOutputs Execute(Dictionary<string, Model> inputModels, FacadeGridByLevelsInputs input)
        {
            var outputModel = new Model();
            var basePanels = new Dictionary<string, Panel>();
            var levelVolumes = inputModels["Levels"].AllElementsOfType<LevelVolume>();
            var defaultSolidMaterial = new Material("White Transparent")
            {
                Color = new Color(1, 1, 1, 0.7),
            };
            var invisibleMaterial = new Material("Invisible")
            {
                Color = new Color(1, 1, 1, 0.0),
            };
            if (levelVolumes.Count() == 0)
            {
                if (inputModels.TryGetValue("Conceptual Mass", out var massModel))
                {
                    levelVolumes = massModel.AllElementsOfType<LevelVolume>();
                }
                else
                {
                    throw new Exception("There were no Level Volumes found in the model. Try using Simple Levels By Envelope to generate levels instead.");
                }
            }
            var facadeGridCells = new List<Panel>();
            foreach (var levelVolume in levelVolumes)
            {
                var profile = levelVolume.Profile;
                if (input.OffsetFromFacade > 0)
                {
                    var perimeter = profile.Perimeter.Offset(input.OffsetFromFacade).First();
                    var voids = profile.Voids.SelectMany(v => v.Offset(input.OffsetFromFacade)).ToList();
                    profile = new Profile(perimeter, voids, Guid.NewGuid(), "OffsetProfile");
                }
                var segments = profile.Perimeter.Segments().Union(profile.Voids.SelectMany(v => v.Segments()));
                foreach (var segment in segments)
                {
                    var edge = new LevelEdge(new List<string>(), levelVolume.Id.ToString(), segment, Guid.NewGuid(), null);
                    var uGrid = new Grid1d(segment);
                    switch (input.Mode)
                    {
                        case FacadeGridByLevelsInputsMode.Approximate_Width:
                            uGrid.DivideByApproximateLength(input.TargetFacadePanelWidth);
                            break;
                        case FacadeGridByLevelsInputsMode.Fixed_Width:
                            if (input.FixedWidthSettings.HeightShift > 0)
                            {
                                uGrid.DivideByFixedLengthFromPosition(input.FixedWidthSettings.PanelWidth, (input.FixedWidthSettings.HeightShift * levelVolume.Transform.Origin.Z + 1.01) % uGrid.Domain.Length);
                            }
                            else
                            {
                                uGrid.DivideByFixedLength(input.FixedWidthSettings.PanelWidth, DivisionMode(input.RemainderPosition));
                            }
                            break;
                        case FacadeGridByLevelsInputsMode.Pattern:
                            uGrid.DivideByPattern(input.PatternSettings.PanelWidthPattern, PatternMode(input.PatternSettings.PatternMode), DivisionMode(input.RemainderPosition));
                            break;
                    }
                    foreach (var cell in uGrid.Cells)
                    {
                        cell.Type = $"{cell.Domain.Length:0.00}";
                    }
                    var vGrid = new Grid1d(new Line(segment.Start, segment.Start + new Vector3(0, 0, levelVolume.Height)));
                    vGrid.Type = $"{levelVolume.Height:0.00}";
                    var grid2d = new Grid2d(uGrid, vGrid);
                    var cells = grid2d.GetCells();
                    foreach (var cell in cells)
                    {
                        if (!basePanels.ContainsKey(cell.Type))
                        {
                            var cellBoundary = new Polygon(new Vector3[] {
                                new Vector3(0,0),
                                new Vector3(cell.U.Domain.Length, 0, 0),
                                new Vector3(cell.U.Domain.Length, cell.V.Domain.Length, 0),
                                new Vector3(0, cell.V.Domain.Length, 0),
                            });
                            var cellColorMaterial = default(Material);
                            switch (input.DisplayMode)
                            {
                                case FacadeGridByLevelsInputsDisplayMode.Color_By_Type:
                                    cellColorMaterial = new Material(cell.Type, NextRandomColor(), 0.0, 0.0, null, true, false);
                                    break;
                                case FacadeGridByLevelsInputsDisplayMode.Edges_Only:
                                    cellColorMaterial = invisibleMaterial;
                                    break;
                                case FacadeGridByLevelsInputsDisplayMode.Solid_Color:
                                    cellColorMaterial = defaultSolidMaterial;
                                    break;
                            }
                            var panel = new Panel(cellBoundary as Polygon, cellColorMaterial, levelVolume.Transform, null, true, Guid.NewGuid(), cell.Type);
                            basePanels.Add(cell.Type, panel);
                        }
                        var panelBoundaryInSpace = cell.GetCellGeometry() as Polygon;
                        var panelBottomEdge = panelBoundaryInSpace.Segments()[0];
                        var panelTransform = new Transform(panelBottomEdge.Start, panelBottomEdge.Direction(), panelBottomEdge.Direction().Cross(Vector3.ZAxis));
                        panelTransform.Concatenate(levelVolume.Transform);
                        var panelInstance = basePanels[cell.Type].CreateInstance(panelTransform, cell.Type);
                        var metadata = new FacadeGridMetadata(basePanels[cell.Type].Area(), Guid.NewGuid(), cell.Type);
                        var boundary = new ModelCurve(cell.GetCellGeometry() as Polygon, null, levelVolume.Transform, null, false, Guid.NewGuid(), cell.Type);
                        outputModel.AddElement(panelInstance);
                        outputModel.AddElement(metadata);
                        edge.PanelIds.Add(panelInstance.Id.ToString());
                        outputModel.AddElement(boundary);
                    }
                    outputModel.AddElement(edge);
                }
            }

            var output = new FacadeGridByLevelsOutputs(basePanels.Count, outputModel.AllElementsOfType<ElementInstance>().Count());
            output.Model = outputModel;
            return output;
        }

        private static PatternMode PatternMode(PatternSettingsPatternMode patternMode)
        {
            switch (patternMode)
            {
                case PatternSettingsPatternMode.Flip:
                    return Elements.Spatial.PatternMode.Flip;
                default:
                case PatternSettingsPatternMode.Cycle:
                    return Elements.Spatial.PatternMode.Cycle;
            }
        }

        private static FixedDivisionMode DivisionMode(FacadeGridByLevelsInputsRemainderPosition remainderPosition)
        {
            switch (remainderPosition)
            {
                case FacadeGridByLevelsInputsRemainderPosition.At_End:
                    return FixedDivisionMode.RemainderAtEnd;
                case FacadeGridByLevelsInputsRemainderPosition.At_Start:
                    return FixedDivisionMode.RemainderAtStart;
                case FacadeGridByLevelsInputsRemainderPosition.At_Middle:
                    return FixedDivisionMode.RemainderNearMiddle;
                case FacadeGridByLevelsInputsRemainderPosition.At_Both_Ends:
                default:
                    return FixedDivisionMode.RemainderAtBothEnds;
            }
        }

        private static Random Random = new Random(5);
        private static Color NextRandomColor()
        {
            return new Color(Random.NextDouble(), Random.NextDouble(), Random.NextDouble(), 1);
        }
    }
}