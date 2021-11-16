using Elements;
using Elements.Geometry;
using Elements.Geometry.Solids;
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
            var hasEnvelopes = inputModels.TryGetValue("Envelope", out var envelopesModel);

            var levelVolumes = inputModels["Levels"].AllElementsOfType<LevelVolume>();
            var couldBeLevelVolumes = inputModels["Levels"].Elements.Values.Where(v => v.GetType().ToString().Contains("LevelVolume"));
            Console.WriteLine($"{couldBeLevelVolumes.Count()} level volumes");
            var assemblyLoc = couldBeLevelVolumes.First().GetType().Assembly.Location;
            Console.WriteLine(assemblyLoc);
            var otherLoc = typeof(LevelVolume).Assembly.Location;
            Console.WriteLine(otherLoc);

            var defaultSolidMaterial = new Material("White Transparent")
            {
                Color = new Color(1, 1, 1, 0.7),
                SpecularFactor = 0.1,
                GlossinessFactor = 0.1,
            };
            var invisibleMaterial = new Material("Invisible")
            {
                Color = new Color(1, 1, 1, 0.0),
                SpecularFactor = 0.1,
                GlossinessFactor = 0.1
            };
            if (levelVolumes.Count() == 0)
            {
                throw new Exception("There were no Level Volumes found in the model. Try using Simple Levels By Envelope to generate levels instead.");
            }
            var facadeGridCells = new List<Panel>();
            var levelVolumesGrouped = levelVolumes.GroupBy(lv =>
            {
                if (lv.AdditionalProperties.TryGetValue("Envelope", out var envId))
                {
                    return new Guid((string)envId);
                }
                else
                {
                    return Guid.Empty;
                }
            });
            foreach (var envelopeGroup in levelVolumesGrouped)
            {
                Element envelopeElem = null;
                envelopesModel?.Elements.TryGetValue(envelopeGroup.Key, out envelopeElem);
                if (envelopeElem is Envelope envelope)
                {
                    var rep = envelope.Representation;
                    var solids = rep.SolidOperations.Select(so => so.Solid);
                    var faces = solids.SelectMany(s => s.Faces.Values);
                    var sideFaces = faces.Where(f => Math.Abs(f.Outer.ToPolygon().Normal().Dot(Vector3.ZAxis)) < 0.7);

                    var levelElevations = envelopeGroup.Select(lv => lv.Transform.Origin.Z).Distinct();

                    foreach (var face in sideFaces)
                    {
                        var outer = face.Outer.ToPolygon();
                        var normal = outer.Normal();
                        var horizontalVector = Vector3.ZAxis.Cross(normal);
                        var polygons = new List<Polygon> { outer };
                        var inner = face.Inner?.Select(i => i.ToPolygon()).ToList();
                        if (inner == null)
                        {
                            inner = new List<Polygon>();
                        }
                        polygons.AddRange(inner);
                        var profile = new Profile(outer, inner);
                        var massFace = new MassFace(profile, envelope.Id);
                        var massFaceSection = new MassFaceSection(profile, massFace.Id);
                        outputModel.AddElements(massFace, massFaceSection);
                        var transform = new Transform(outer.Vertices[0], horizontalVector, normal);
                        var inverse = transform.Inverted();
                        var grid2d = new Grid2d(polygons, transform);
                        var centroid = outer.Centroid();
                        var plane = new Plane(centroid, normal);
                        var otherPlane = new Plane(centroid, horizontalVector);
                        var points = new List<Vector3>();
                        foreach (var levelElev in levelElevations)
                        {
                            var elevPlane = new Plane((0, 0, levelElev), Vector3.ZAxis);
                            if (plane.Intersects(otherPlane, elevPlane, out var intersection))
                            {
                                points.Add(intersection);
                            }
                        }
                        grid2d.V.SplitAtPoints(points);
                        var uGrid = grid2d.U;

                        switch (input.Mode)
                        {
                            case FacadeGridByLevelsInputsMode.Approximate_Width:
                                uGrid.DivideByApproximateLength(input.TargetFacadePanelWidth);
                                break;
                            case FacadeGridByLevelsInputsMode.Fixed_Width:
                                if (input.FixedWidthSettings.HeightShift > 0)
                                {
                                    uGrid.DivideByFixedLengthFromPosition(input.FixedWidthSettings.PanelWidth, (input.FixedWidthSettings.HeightShift) % uGrid.Domain.Length);
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
                        foreach (var cell in grid2d.V.Cells)
                        {
                            cell.Type = $"{cell.Domain.Length:0.00}";
                        }

                        InstancesFromGrid2d(input, outputModel, basePanels, defaultSolidMaterial, invisibleMaterial, envelopeElem, grid2d);
                    }
                }
                // old levelvolume-based code pathway
                // foreach (var levelVolume in envelopeGroup)
                // {
                //     var profile = levelVolume.Profile;
                //     var lvlHasEnvelope = levelVolume.AdditionalProperties.TryGetValue("Envelope", out var envelopeId);
                //     if (input.OffsetFromFacade > 0)
                //     {
                //         var perimeter = profile.Perimeter.Offset(input.OffsetFromFacade).First();
                //         var voids = profile.Voids.SelectMany(v => v.Offset(input.OffsetFromFacade)).ToList();
                //         profile = new Profile(perimeter, voids, Guid.NewGuid(), "OffsetProfile");
                //     }
                //     var segments = profile.Perimeter.Segments().Union(profile.Voids.SelectMany(v => v.Segments()));
                //     foreach (var segment in segments)
                //     {
                //         var heightVector = (0, 0, levelVolume.Height);

                //         var edge = new LevelEdge(new List<string>(), levelVolume.Id.ToString(), segment, Guid.NewGuid(), null);
                //         var uGrid = new Grid1d(segment);
                //         switch (input.Mode)
                //         {
                //             case FacadeGridByLevelsInputsMode.Approximate_Width:
                //                 uGrid.DivideByApproximateLength(input.TargetFacadePanelWidth);
                //                 break;
                //             case FacadeGridByLevelsInputsMode.Fixed_Width:
                //                 if (input.FixedWidthSettings.HeightShift > 0)
                //                 {
                //                     uGrid.DivideByFixedLengthFromPosition(input.FixedWidthSettings.PanelWidth, (input.FixedWidthSettings.HeightShift * levelVolume.Transform.Origin.Z + 1.01) % uGrid.Domain.Length);
                //                 }
                //                 else
                //                 {
                //                     uGrid.DivideByFixedLength(input.FixedWidthSettings.PanelWidth, DivisionMode(input.RemainderPosition));
                //                 }
                //                 break;
                //             case FacadeGridByLevelsInputsMode.Pattern:
                //                 uGrid.DivideByPattern(input.PatternSettings.PanelWidthPattern, PatternMode(input.PatternSettings.PatternMode), DivisionMode(input.RemainderPosition));
                //                 break;
                //         }
                //         foreach (var cell in uGrid.Cells)
                //         {
                //             cell.Type = $"{cell.Domain.Length:0.00}";
                //         }
                //         var vGrid = new Grid1d(new Line(segment.Start, segment.Start + heightVector))
                //         {
                //             Type = $"{levelVolume.Height:0.00}"
                //         };
                //         var grid2d = new Grid2d(uGrid, vGrid);
                //         InstancesFromGrid2d(input, outputModel, basePanels, defaultSolidMaterial, invisibleMaterial, envelopeElem, levelVolume, edge, grid2d);
                //         outputModel.AddElement(edge);
                //     }
                // }

            }


            var output = new FacadeGridByLevelsOutputs(basePanels.Count, outputModel.AllElementsOfType<ElementInstance>().Count())
            {
                Model = outputModel
            };
            return output;
        }

        private static void InstancesFromGrid2d(FacadeGridByLevelsInputs input, Model outputModel, Dictionary<string, Panel> basePanels, Material defaultSolidMaterial, Material invisibleMaterial, Element envelopeElem, Grid2d grid2d)
        {
            var cells = grid2d.GetCells();
            foreach (var cell in cells)
            {
                if (cell.IsTrimmed())
                {
                    var type = $"{cell.Type}-irreg";
                    var trimmedGeo = cell.GetTrimmedCellGeometry().OfType<Polygon>().ToList();
                    if (trimmedGeo.Count == 0)
                    {
                        continue;
                    }
                    var profile = new Profile(trimmedGeo);
                    var irregMetadata = new FacadeGridMetadata(profile.Perimeter.Area(), Guid.NewGuid(), type);
                    var cellColorMaterial = MatForCell(input, defaultSolidMaterial, invisibleMaterial, cell.Type + irregMetadata.Id.ToString());

                    outputModel.AddElements(profile.ToModelCurves());
                    outputModel.AddElement(irregMetadata);
                    var panel = new Panel(profile.Perimeter, cellColorMaterial, new Transform(), new Lamina(profile), false, Guid.NewGuid(), type);
                    panel.AdditionalProperties["Envelope"] = envelopeElem.Id;
                    outputModel.AddElement(panel);
                    continue;
                }
                if (!basePanels.ContainsKey(cell.Type))
                {
                    var cellBoundary = new Polygon(new Vector3[] {
                                new Vector3(0,0),
                                new Vector3(cell.U.Domain.Length, 0, 0),
                                new Vector3(cell.U.Domain.Length, cell.V.Domain.Length, 0),
                                new Vector3(0, cell.V.Domain.Length, 0),
                            });
                    var cellColorMaterial = MatForCell(input, defaultSolidMaterial, invisibleMaterial, cell.Type);
                    var panel = new Panel(cellBoundary, cellColorMaterial, new Transform(), null, true, Guid.NewGuid(), cell.Type);
                    basePanels.Add(cell.Type, panel);
                }
                var panelBoundaryInSpace = cell.GetCellGeometry() as Polygon;
                var normal = panelBoundaryInSpace.Normal();
                var panelBottomEdge = panelBoundaryInSpace.Segments()[0];
                var panelTransform = new Transform(panelBottomEdge.Start, panelBottomEdge.Direction(), normal);
                var panelInstance = basePanels[cell.Type].CreateInstance(panelTransform, cell.Type);
                panelInstance.AdditionalProperties["Envelope"] = envelopeElem.Id;
                var metadata = new FacadeGridMetadata(basePanels[cell.Type].Area(), Guid.NewGuid(), cell.Type);
                var boundary = new ModelCurve(panelBoundaryInSpace) { Name = cell.Type };
                outputModel.AddElement(panelInstance);
                outputModel.AddElement(metadata);
                outputModel.AddElement(boundary);
            }
        }

        private static Material MatForCell(FacadeGridByLevelsInputs input, Material defaultSolidMaterial, Material invisibleMaterial, string cellType)
        {
            var cellColorMaterial = default(Material);
            switch (input.DisplayMode)
            {
                case FacadeGridByLevelsInputsDisplayMode.Color_By_Type:
                    cellColorMaterial = new Material(cellType, NextRandomColor(), 0.0, 0.0, null, true, false);
                    break;
                case FacadeGridByLevelsInputsDisplayMode.Edges_Only:
                    cellColorMaterial = invisibleMaterial;
                    break;
                case FacadeGridByLevelsInputsDisplayMode.Solid_Color:
                    cellColorMaterial = defaultSolidMaterial;
                    break;
            }

            return cellColorMaterial;
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
            return remainderPosition switch
            {
                FacadeGridByLevelsInputsRemainderPosition.At_End => FixedDivisionMode.RemainderAtEnd,
                FacadeGridByLevelsInputsRemainderPosition.At_Start => FixedDivisionMode.RemainderAtStart,
                FacadeGridByLevelsInputsRemainderPosition.At_Middle => FixedDivisionMode.RemainderNearMiddle,
                _ => FixedDivisionMode.RemainderAtBothEnds,
            };
        }

        private static readonly Random Random = new Random(5);
        private static Color NextRandomColor()
        {
            return new Color(Random.NextDouble(), Random.NextDouble(), Random.NextDouble(), 1);
        }
    }
}