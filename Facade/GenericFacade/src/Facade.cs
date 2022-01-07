using Elements;
using Elements.Geometry;
using Elements.Geometry.Solids;
using Elements.Spatial;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Facade
{
    public static class Facade
    {
        /// <summary>
        /// The Facade function.
        /// </summary>
        /// <param name="model">The input model.</param>
        /// <param name="input">The arguments to the execution.</param>
        /// <returns>A FacadeOutputs instance containing computed results and the model with any new elements.</returns>
        public static FacadeOutputs Execute(Dictionary<string, Model> inputModels, FacadeInputs input)
        {
            var output = new FacadeOutputs();
            var facadeTypes = inputModels["Facade Grid"].AllElementsOfType<FacadeType>();
            var panelDefinitions = inputModels["Facade Grid"].AllElementsOfType<Panel>();
            var panelInstances = inputModels["Facade Grid"].AllElementsOfType<ElementInstance>();
            foreach (var ft in facadeTypes)
            {
                var proxy = ft.Proxy("Facade Grid");
                FacadeTypeSettingsValue panelSettings = CreateDefaultPanelSettings();
                if (input.Overrides?.FacadeTypeSettings != null)
                {
                    var matchingOverride = input.Overrides.FacadeTypeSettings.FirstOrDefault(s => s.Identity.Name == ft.Name);
                    if (matchingOverride != null)
                    {
                        Identity.AddOverrideValue(proxy, matchingOverride.GetName(), matchingOverride.Value);
                        panelSettings = matchingOverride.Value;
                    }
                }

                if (panelSettings.CreateGenericFacade == false)
                {
                    continue;
                }

                var matchingPanels = panelDefinitions.Where(pd => pd.Name.StartsWith(ft.Name + " - "));
                // create base elements for spandrel, mullions, and panel from panelSettings.
                // these will only be used for non-irregular panels.

                // create base panel
                var col = panelSettings.Material.Color;
                col.Alpha = panelSettings.Material.Opacity;
                var panelMaterial = new Material(ft.Name) { Color = col, GlossinessFactor = panelSettings.Material.Shine };
                var baseRect = Polygon.Rectangle((0, 0), (1, 1));
                var basePanel = new FacadePanel(baseRect)
                {
                    Material = panelMaterial,
                    IsElementDefinition = true,
                    Thickness = !panelSettings.HorizontalSpandrel && !panelSettings.VerticalSpandrel ? panelSettings.Thickness : 0,
                };
                Mullion baseMullion = null;
                // create mullion
                if (panelSettings.Mullions)
                {
                    var mullionCol = panelSettings.MullionSettings.Material.Color;
                    var mullionMaterial = new Material(ft.Name + " Mullion")
                    {
                        Color = mullionCol,
                        GlossinessFactor = panelSettings.MullionSettings.Material.Shine
                    };
                    var width = panelSettings.MullionSettings.Width;
                    var depth = panelSettings.MullionSettings.Depth;
                    var baseMullionRect = Polygon.Rectangle((-width / 2, 0), (width / 2, 1));
                    var extrude = new Extrude(baseMullionRect, depth, Vector3.ZAxis, false);
                    baseMullion = new Mullion()
                    {
                        Representation = extrude,
                        Material = mullionMaterial,
                        IsElementDefinition = true
                    };
                }
                Spandrel baseHSpandrel = null;
                // create horizontal spandrel
                if (panelSettings.HorizontalSpandrel)
                {
                    var hSpandrelCol = panelSettings.HorizontalSpandrelSettings.Material.Color;
                    var hSpandrelMat = new Material(ft.Name + "Horizontal Spandrel")
                    {
                        Color = hSpandrelCol,
                        GlossinessFactor = panelSettings.HorizontalSpandrelSettings.Material.Shine
                    };
                    var hSpandrelRect = Polygon.Rectangle((0, 0), (1, panelSettings.HorizontalSpandrelSettings.Height));
                    var extrude = new Extrude(hSpandrelRect, Math.Max(panelSettings.Thickness, 0.1), Vector3.ZAxis, false);
                    baseHSpandrel = new Spandrel()
                    {
                        Representation = extrude,
                        Material = hSpandrelMat,
                        IsElementDefinition = true
                    };
                }

                Spandrel baseVSpandrel = null;
                if (panelSettings.VerticalSpandrel)
                {
                    var vSpandrelCol = panelSettings.VerticalSpandrelSettings.Material.Color;
                    var vSpandrelMat = new Material(ft.Name + "Vertical Spandrel")
                    {
                        Color = vSpandrelCol,
                        GlossinessFactor = panelSettings.VerticalSpandrelSettings.Material.Shine
                    };
                    var vSpandrelRect = Polygon.Rectangle((0, 0), (panelSettings.VerticalSpandrelSettings.Width, 1));
                    var extrude = new Extrude(vSpandrelRect, Math.Max(panelSettings.Thickness, 0.1), Vector3.ZAxis, false);
                    baseVSpandrel = new Spandrel()
                    {
                        Representation = extrude,
                        Material = vSpandrelMat,
                        IsElementDefinition = true
                    };
                }
                var baseElements = (basePanel, baseMullion, baseHSpandrel, baseVSpandrel);

                foreach (var mp in matchingPanels)
                {
                    InstantiatePanelsForFacadeType(output, panelInstances, ft, panelSettings, baseElements, mp);
                }

                output.Model.AddElement(proxy);
            }
            return output;
        }

        private static void InstantiatePanelsForFacadeType(FacadeOutputs output, IEnumerable<ElementInstance> panelInstances, FacadeType ft, FacadeTypeSettingsValue panelSettings, (Panel basePanel, Mullion baseMullion, Spandrel baseHSpandrel, Spandrel baseVSpandrel) baseElements, Panel mp)
        {
            if (mp.IsElementDefinition)
            {
                var matchingInstances = panelInstances.Where(i => i.BaseDefinition.Id == mp.Id);
                foreach (var mi in matchingInstances)
                {
                    if (mp.Name.Contains("irreg"))
                    {
                        var grid = new Grid2d(mp.Perimeter, new Transform());
                        var panel = new FacadePanel(mp.Perimeter)
                        {
                            Material = baseElements.basePanel.Material,
                            IsElementDefinition = true,
                            Thickness = !panelSettings.HorizontalSpandrel && !panelSettings.VerticalSpandrel ? panelSettings.Thickness : 0,
                        };
                        var panelInstance = panel.CreateInstance(mi.Transform, mi.Name);
                        foreach (var kvp in mi.AdditionalProperties)
                        {
                            panelInstance.AdditionalProperties[kvp.Key] = kvp.Value;
                        }
                        output.Model.AddElement(panelInstance);
                        if (baseElements.baseVSpandrel != null)
                        {
                            grid.U.SplitAtOffset(panelSettings.VerticalSpandrelSettings.Width, ignoreOutsideDomain: true);
                            var vSpandrelBoundary = grid[0, 0];
                            var boundary = vSpandrelBoundary.GetTrimmedCellGeometry().OfType<Polygon>().OrderBy(c => c.Area()).LastOrDefault();
                            if (boundary != null)
                            {
                                var spandrel = new Spandrel()
                                {
                                    Material = baseElements.baseVSpandrel.Material,
                                    Representation = new Extrude(boundary, Math.Max(panelSettings.Thickness, 0.1), Vector3.ZAxis, false),
                                    IsElementDefinition = true
                                };
                                var spandrelInstance = spandrel.CreateInstance(mi.Transform, mi.Name);
                                foreach (var kvp in mi.AdditionalProperties)
                                {
                                    spandrelInstance.AdditionalProperties[kvp.Key] = kvp.Value;
                                }
                                output.Model.AddElement(spandrelInstance);
                            }
                        }
                        if (baseElements.baseHSpandrel != null)
                        {
                            var remainingCell = grid.GetRowAtIndex(0).Last();
                            remainingCell.V.SplitAtOffset(panelSettings.HorizontalSpandrelSettings.Height, ignoreOutsideDomain: true);
                            var hSpandrelBoundary = remainingCell.CellsFlat.First();
                            var boundary = hSpandrelBoundary.GetTrimmedCellGeometry().OfType<Polygon>().OrderBy(c => c.Area()).LastOrDefault();
                            if (boundary != null)
                            {
                                var spandrel = new Spandrel()
                                {
                                    Material = baseElements.baseHSpandrel.Material,
                                    Representation = new Extrude(boundary, Math.Max(panelSettings.Thickness, 0.1), Vector3.ZAxis, false),
                                    IsElementDefinition = true
                                };
                                var spandrelInstance = spandrel.CreateInstance(mi.Transform, mi.Name);
                                foreach (var kvp in mi.AdditionalProperties)
                                {
                                    spandrelInstance.AdditionalProperties[kvp.Key] = kvp.Value;
                                }
                                output.Model.AddElement(spandrelInstance);
                            }
                        }
                        if (baseElements.baseMullion != null)
                        {
                            foreach (var segment in mp.Perimeter.Segments())
                            {
                                MullionFromLine(output, baseElements, mi, segment);
                            }
                            MullionsFromGridSeparators(output, baseElements, mi, grid, GridDirection.V);
                            MullionsFromGridSeparators(output, baseElements, mi, grid, GridDirection.U);
                        }
                    }
                    else
                    {
                        var segments = mp.Perimeter.Segments();
                        var widthSeg = segments.First();
                        var heightSeg = segments.Last();
                        var width = widthSeg.Length();
                        var height = heightSeg.Length();
                        var origin = widthSeg.Start;
                        // create panel
                        var panelXform = new Transform().Scaled(new Vector3(width, height, 1)).Concatenated(mi.Transform);
                        var panelInstance = baseElements.basePanel.CreateInstance(panelXform, mi.Name);
                        foreach (var kvp in mi.AdditionalProperties)
                        {
                            panelInstance.AdditionalProperties[kvp.Key] = kvp.Value;
                        }
                        output.Model.AddElement(panelInstance);

                        (Vector3 from, Vector3 to) remainingArea = ((0, 0), (width, height));
                        // create spandrels

                        if (baseElements.baseVSpandrel != null)
                        {
                            var spandrelWidth = panelSettings.VerticalSpandrelSettings.Width;
                            var widthFactor = 1.0;
                            // we should cover the whole thing with a vertical spandrel if it's too narrow
                            if (width < spandrelWidth * 2)
                            {
                                widthFactor = width / spandrelWidth;
                            }
                            var vSpandrelXForm = new Transform().Scaled((widthFactor, height, 1)).Concatenated(mi.Transform);
                            var vSpandrelInstance = baseElements.baseVSpandrel.CreateInstance(vSpandrelXForm, mi.Name);
                            foreach (var kvp in mi.AdditionalProperties)
                            {
                                vSpandrelInstance.AdditionalProperties[kvp.Key] = kvp.Value;
                            }
                            output.Model.AddElement(vSpandrelInstance);
                            remainingArea = ((spandrelWidth * widthFactor, 0), remainingArea.to);
                        }
                        var remainingWidth = remainingArea.to.X - remainingArea.from.X;
                        if (baseElements.baseHSpandrel != null && remainingWidth > 0.01)
                        {
                            var spandrelHeight = panelSettings.HorizontalSpandrelSettings.Height;
                            var heightFactor = 1.0;
                            if (height < spandrelHeight * 2)
                            {
                                // we should cover the whole thing with a horizontal spandrel if it's too short
                                heightFactor = height / spandrelHeight;
                            }
                            var spandrelXform = new Transform().Scaled((remainingWidth, heightFactor, 1)).Moved(remainingArea.from).Concatenated(mi.Transform);
                            var spandrelInstance = baseElements.baseHSpandrel.CreateInstance(spandrelXform, mi.Name);
                            foreach (var kvp in mi.AdditionalProperties)
                            {
                                spandrelInstance.AdditionalProperties[kvp.Key] = kvp.Value;
                            }
                            output.Model.AddElement(spandrelInstance);
                            remainingArea = ((remainingArea.from.X, spandrelHeight * heightFactor), (width, height));
                        }
                        var remainingHeight = remainingArea.to.Y - remainingArea.from.Y;
                        // create mullions
                        if (baseElements.baseMullion != null)
                        {
                            var rect = Polygon.Rectangle((0, 0), (width, height));
                            var grid = new Grid2d(rect);
                            grid.SplitAtPoint(remainingArea.from);
                            if (remainingWidth > 0.1 && remainingHeight > 0.1)
                            {
                                var innerRect = Polygon.Rectangle(remainingArea.from, remainingArea.to);
                                var innerGrid = new Grid2d(innerRect);
                                if (panelSettings.MullionSettings.HorizontalSpacing != null)
                                {
                                    innerGrid.U.DivideByApproximateLength(panelSettings.MullionSettings.HorizontalSpacing.Value);
                                    MullionsFromGridSeparators(output, baseElements, mi, innerGrid, GridDirection.V);

                                }
                                if (panelSettings.MullionSettings.VerticalSpacing != null)
                                {
                                    innerGrid.V.DivideByApproximateLength(panelSettings.MullionSettings.VerticalSpacing.Value);
                                    MullionsFromGridSeparators(output, baseElements, mi, innerGrid, GridDirection.U);

                                }
                            }
                            MullionsFromGridSeparators(output, baseElements, mi, grid, GridDirection.V);
                            MullionsFromGridSeparators(output, baseElements, mi, grid, GridDirection.U);
                        }
                        // output.Model.AddElement(new ModelCurve(widthSeg.TransformedLine(mi.Transform), BuiltInMaterials.XAxis));
                        // output.Model.AddElement(new ModelCurve(heightSeg.TransformedLine(mi.Transform), BuiltInMaterials.YAxis));
                    }
                    // var instance = newPanel.CreateInstance(mi.Transform, mi.Name);
                    // foreach (var kvp in mi.AdditionalProperties)
                    // {
                    //     instance.AdditionalProperties[kvp.Key] = kvp.Value;
                    // }
                    // output.Model.AddElement(instance);
                }
            }
        }

        private static void MullionsFromGridSeparators(FacadeOutputs output, (Panel basePanel, Mullion baseMullion, Spandrel baseHSpandrel, Spandrel baseVSpandrel) baseElements, ElementInstance mi, Grid2d grid, GridDirection dir)
        {
            foreach (var mullionLine in grid.GetCellSeparators(dir, true).OfType<Line>())
            {
                MullionFromLine(output, baseElements, mi, mullionLine);
            }
        }

        private static void MullionFromLine(FacadeOutputs output, (Panel basePanel, Mullion baseMullion, Spandrel baseHSpandrel, Spandrel baseVSpandrel) baseElements, ElementInstance mi, Line line)
        {
            var length = line.Length();
            var scaleAndRotationXform = new Transform().Scaled((1, length, 1));
            var rotationAngle = line.Direction().PlaneAngleTo(Vector3.YAxis);

            scaleAndRotationXform.Rotate(Vector3.ZAxis, -rotationAngle);

            var mullionXform = scaleAndRotationXform
                .Concatenated(new Transform(line.Start))
                .Concatenated(mi.Transform);
            var mullionInstance = baseElements.baseMullion.CreateInstance(mullionXform, mi.Name);
            foreach (var kvp in mi.AdditionalProperties)
            {
                mullionInstance.AdditionalProperties[kvp.Key] = kvp.Value;
            }
            output.Model.AddElement(mullionInstance);
        }

        private static FacadeTypeSettingsValue CreateDefaultPanelSettings()
        {
            return new FacadeTypeSettingsValue(
                    true,
                    new FacadeTypeSettingsValueMaterial(
                      new Color(0.7, 0.7, 1.0, 1.0),
                      0.5,
                      0
                    ),
                    0,
                    0,
                    false,
                    null,
                    false,
                    null,
                    false,
                    null
                  );
        }
    }
}