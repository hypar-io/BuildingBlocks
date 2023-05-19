using Elements;
using Elements.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ColumnsFromGrid
{
    public static class ColumnsFromGrid
    {
        private static readonly string gridsDependencyName = GridlinesOptionsOverride.Dependency;
        private static readonly List<WarningMessage> warnings = new List<WarningMessage>();
        private static readonly List<Element> proxies = new List<Element>();

        /// <summary>
        /// The ColumnsFromGrid function.
        /// </summary>
        /// <param name="model">The input model.</param>
        /// <param name="input">The arguments to the execution.</param>
        /// <returns>A ColumnsFromGridOutputs instance containing computed results and the model with any new elements.</returns>
        public static ColumnsFromGridOutputs Execute(Dictionary<string, Model> inputModels, ColumnsFromGridInputs input)
        {
            var output = new ColumnsFromGridOutputs();
            warnings.Clear();
            inputModels.TryGetValue("Structure", out var structureModel);
            inputModels.TryGetValue("Grids", out var gridsModel);

            var sections = new List<DrainableRoofSection>();
            if (inputModels.TryGetValue("Drainable Roof Sections", out var envelopeModel))
            {
                sections = envelopeModel.AllElementsOfType<DrainableRoofSection>().ToList();
            }

            var levelVolumes = new List<LevelVolume>();
            if (inputModels.TryGetValue("Levels", out var levelsModel))
            {
                levelVolumes = levelsModel.AllElementsOfType<LevelVolume>().ToList();
            }
            var envelopePolygons = GetEnvelopePolygons(inputModels);
            var grids = gridsModel?.AllElementsOfType<Grid2dElement>() ?? new List<Grid2dElement>();
            var columns = new List<Column>();
            if (structureModel != null)
            {
                columns = CreateColumnsFromStructure(input, structureModel, sections);
            }
            if (!columns.Any() && gridsModel != null)
            {
                columns = CreateColumnsFromGrid(input, gridsModel, sections, envelopePolygons, levelVolumes, grids);
            }
            columns.AddRange(CreateAdditionalColumns(input, sections, levelVolumes, grids, columns.Select(c => c.Name).ToHashSet()));

            output.Model.AddElements(columns);
            output.Model.AddElements(proxies);
            output.Model.AddElements(warnings);

            return output;
        }

        private static List<Column> CreateColumnsFromStructure(ColumnsFromGridInputs input, Model structureModel, List<DrainableRoofSection> sections)
        {
            var columns = new List<Column>();
            var columnInstances = structureModel.AllElementsOfType<ElementInstance>().Where(e => e.BaseDefinition is Column);
            var i = 1;
            foreach (var columnInstance in columnInstances)
            {
                var columnDefinition = columnInstance.BaseDefinition as Column;
                var bounds = columnDefinition.Profile.Perimeter.Bounds();
                var size = new SizesValue(bounds.XSize, columnDefinition.Height, bounds.YSize, input.FinishThickness);
                var column = CreateColumn(size, input, columnInstance.Transform.Concatenated(new Transform(columnInstance.Transform.Origin).Inverted()), columnInstance.Transform.Origin, $"Column-{i}", columnDefinition.Height, sections);
                columns.Add(column);
                i++;
            }

            return columns;
        }

        private static List<Column> CreateColumnsFromGrid(ColumnsFromGridInputs input, Model gridsModel, List<DrainableRoofSection> sections, List<Polygon> envelopePolygons, IEnumerable<LevelVolume> levelVolumes, IEnumerable<Grid2dElement> grids)
        {
            var allGridLines = gridsModel.AllElementsOfType<GridLine>();
            var allGridLinesProxies = allGridLines.Proxies(gridsDependencyName);
            var overrides = input.Overrides?.GridlinesOptions ?? new List<GridlinesOptionsOverride>();
            List<string> createColumnsGridLines = GetEnabledByDefaultGridLines(input, grids, allGridLines);
            AttachAllGridLinesOverrides(allGridLinesProxies, overrides, createColumnsGridLines);
            var defaultSizeValue = new SizesValue(input.Width, input.Height, input.Depth, input.FinishThickness);

            var columns = new List<Column>();
            foreach (var grid in grids)
            {
                var alignedTransform = grid.Transform.Moved(-1 * grid.Transform.Origin);
                foreach (var gridNode in grid.GridNodes)
                {
                    var uConfig = GetGridLineConfigForGridNode(allGridLinesProxies, overrides, gridNode.UGridline, createColumnsGridLines);
                    var vConfig = GetGridLineConfigForGridNode(allGridLinesProxies, overrides, gridNode.VGridline, createColumnsGridLines);

                    bool createColumn = uConfig.CreateColumns && vConfig.CreateColumns;

                    if (input.PerimeterColumns && envelopePolygons.Any())
                    {
                        if (envelopePolygons.Any(e => gridNode.Location.Origin.DistanceTo(e as Polyline) < 0.01))
                        {
                            createColumn = true;
                        }
                    }
                    else if (!input.CreateAllColumns)
                    {
                        if (createColumnsGridLines.Contains(gridNode.UGridline) || createColumnsGridLines.Contains(gridNode.VGridline))
                        {
                            createColumn = uConfig.CreateColumns || vConfig.CreateColumns;
                        }
                    }

                    if (createColumn && envelopePolygons.Any())
                    {
                        // Check if the grid node is inside the envelope.
                        createColumn = envelopePolygons.Any(e => e.Contains(gridNode.Location.Origin, out var containment) || containment == Containment.CoincidesAtEdge || containment == Containment.CoincidesAtVertex);
                    }
                    var columnIsNotRemoved = input.Overrides?.Removals?.ColumnPositions == null
                                             || !input.Overrides.Removals.ColumnPositions
                                                 .Any(p => p.Identity.Name.Equals(gridNode.Name));
                    if (createColumn && columnIsNotRemoved)
                    {
                        if (levelVolumes.Any())
                        {
                            foreach (var level in levelVolumes)
                            {
                                // Check if the grid node is inside the level.
                                var profile = level.Profile;
                                profile.Contains(gridNode.Location.Origin, out var containment);
                                if (containment == Containment.Inside || containment == Containment.CoincidesAtEdge || containment == Containment.CoincidesAtVertex)
                                {
                                    columns.Add(CreateColumn(defaultSizeValue, input, alignedTransform, gridNode.Location.Origin + (0, 0, level.Transform.Origin.Z), gridNode.Name, level.Height, sections));
                                }
                            }
                        }
                        else
                        {
                            columns.Add(CreateColumn(defaultSizeValue, input, alignedTransform, gridNode.Location.Origin, gridNode.Name, null, sections));
                        }
                    }
                }
            }

            return columns;
        }

        private static List<Column> CreateAdditionalColumns(ColumnsFromGridInputs input, List<DrainableRoofSection> sections, List<LevelVolume> levelVolumes, IEnumerable<Grid2dElement> grids, HashSet<string> usedColumnNames)
        {
            var createdColumns = new List<Column>();
            if (input.Overrides?.Additions?.ColumnPositions != null)
            {
                var defaultSizeValue = new SizesValue(input.Width, input.Height, input.Depth, input.FinishThickness);
                var allGridNodes = grids.SelectMany(grid => grid.GridNodes);
                var i = 1;
                foreach (var newColumn in input.Overrides.Additions.ColumnPositions)
                {
                    var location = newColumn.Value.Location;
                    double? height = null;
                    if (levelVolumes.Any())
                    {
                        var levelAtElevation = levelVolumes.OrderBy(lvl => Math.Abs(lvl.Transform.Origin.Z - location.Z)).FirstOrDefault();
                        if (levelAtElevation != null)
                        {
                            height = levelAtElevation.Height;
                        }
                    }
                    var name = GetColumnName(location, allGridNodes, usedColumnNames, i);
                    var column = CreateColumn(defaultSizeValue, input, new Transform(), location, name, height, sections);
                    Identity.AddOverrideIdentity(column, newColumn);
                    createdColumns.Add(column);
                    i++;
                }
            }

            return createdColumns;
        }

        private static List<string> GetEnabledByDefaultGridLines(ColumnsFromGridInputs input, IEnumerable<Grid2dElement> grids, IEnumerable<GridLine> gridLines)
        {
            var enabledGridLines = new List<string>();
            if (input.CreateAllColumns)
            {
                enabledGridLines.AddRange(grids.SelectMany(g => g.UGridLines));
                enabledGridLines.AddRange(grids.SelectMany(g => g.VGridLines));
            }
            else if (input.PerimeterColumns)
            {
                foreach (var grid in grids)
                {
                    if (grid.UGridLines == null || grid.VGridLines == null)
                    {
                        var boundary = grid.Grid.GetCellGeometry().ToPolyline();
                        var poly = new Polygon(boundary.Vertices);
                        if (poly != null)
                        {
                            var warning = Warnings.WarningAtPolygon("Perimeter gridlines could not be found for grid {0}.", poly as Polygon, name: grid.Name);
                            warning.AdditionalProperties["Grid"] = grid.Id;
                            warnings.Add(warning);
                        }
                        else
                        {
                            var warning = Warnings.TextWarning("Perimeter gridlines could not be found for grid {0}.", grid.Name);
                            warning.AdditionalProperties["Grid"] = grid.Id;
                            warnings.Add(warning);
                        }
                        continue;
                    }
                    enabledGridLines.Add(grid.UGridLines.First());
                    if (grid.UGridLines.Count > 1)
                    {
                        enabledGridLines.Add(grid.UGridLines.Last());
                    }
                    enabledGridLines.Add(grid.VGridLines.First());
                    if (grid.VGridLines.Count > 1)
                    {
                        enabledGridLines.Add(grid.VGridLines.Last());
                    }

                }
            }

            return enabledGridLines;
        }

        private static GridlinesOptionsValue GetGridLineConfigForGridNode(IEnumerable<ElementProxy<GridLine>> allGridLinesProxies, IList<GridlinesOptionsOverride> overrides, string gridLineId, IList<string> perimeterGridlinesIds)
        {
            var gridLineProxy = allGridLinesProxies.FirstOrDefault(g => g.Element.Id.Equals(Guid.Parse(gridLineId)));
            var config = FindGridLineOverride(overrides, gridLineProxy, perimeterGridlinesIds).Value;
            return config;
        }

        private static Column CreateColumn(SizesValue defaultSizeValue, ColumnsFromGridInputs input, Transform alignedTransform, Vector3 location, string name, double? height, List<DrainableRoofSection> sections)
        {
            var locationOverride = input.Overrides?.ColumnPositions?.FirstOrDefault(p => p.Identity.Name.Equals(name) && (height == null || p.Identity.LevelElevation == height));
            var sizeOverride = input.Overrides?.Sizes?.FirstOrDefault(p => p.Identity.Name.Equals(name) && (height == null || p.Identity.LevelElevation == height));
            var sizeOverrideValue = sizeOverride?.Value;
            var resultLocation = locationOverride?.Value.Location ?? location;
            var width = (sizeOverrideValue ?? defaultSizeValue).Width;
            var depth = (sizeOverrideValue ?? defaultSizeValue).Depth;
            var finishThickness = (sizeOverrideValue ?? defaultSizeValue).FinishThickness;
            var levelElevation = height;
            if (sizeOverrideValue != null)
            {
                height = sizeOverrideValue.Height;
            }
            else if (height == null)
            {
                var sectionHeights = sections
                                        .Select(s => new { section = s, projectedLoc = resultLocation.ProjectAlong(Vector3.ZAxis, s.Boundary.Plane()) })
                                        .Where(s => s.section.Boundary.Contains(s.projectedLoc, out _))
                                        .Select(v => v.projectedLoc.Z);
                height = sectionHeights.Any() ? sectionHeights.Max() : defaultSizeValue.Height;
            }
            if (height == 0)
            {
                height = defaultSizeValue.Height;
            }
            var currentAlignedPerim = Polygon.Rectangle(width + finishThickness, depth + finishThickness).TransformedPolygon(alignedTransform);
            var column = new Column(resultLocation,
                            height.Value,
                            null,
                            currentAlignedPerim,
                            material: BuiltInMaterials.Steel,
                            id: Guid.NewGuid(),
                            name: name);

            if (locationOverride != null)
            {
                column.AddOverrideIdentity(ColumnPositionsOverride.Name, locationOverride.Id, locationOverride.Identity);
            }
            if (sizeOverride != null)
            {
                column.AddOverrideIdentity(SizesOverride.Name, sizeOverride.Id, sizeOverride.Identity);
            }
            column.AdditionalProperties["Width"] = width;
            column.AdditionalProperties["Depth"] = depth;
            column.AdditionalProperties["Height"] = height;
            column.AdditionalProperties["Finish Thickness"] = finishThickness;
            if (levelElevation.HasValue)
            {
                column.AdditionalProperties["Level Elevation"] = levelElevation;
            }

            return column;
        }

        private static List<Polygon> GetEnvelopePolygons(Dictionary<string, Model> inputModels)
        {
            var envelopePolygons = new List<Polygon>();
            if (inputModels.TryGetValue("Envelope", out var envelopeModel))
            {
                var envelopes = envelopeModel.AllElementsOfType<Envelope>();
                // Handle all envelopes which are extrusions. This is the old way.
                if (envelopes.All(e => e.Profile != null))
                {
                    envelopePolygons = envelopes.Select(e => (Polygon)e.Profile.Perimeter.Transformed(e.Transform)).ToList();
                }
                // Handle envelopes of all shapes by using a more general method of convex hull projection onto the XY plane.
                else
                {
                    foreach (var e in envelopes)
                    {
                        var polygon = ConvexHull.FromPoints(e.Representation.SolidOperations.SelectMany(o => o.Solid.Vertices.Select(v => new Vector3(v.Value.Point.X, v.Value.Point.Y))));
                        envelopePolygons.Add(polygon);
                    }
                }
            }

            return envelopePolygons;
        }

        private static string GetColumnName(Vector3 location, IEnumerable<GridNode> allGridNodes, HashSet<string> usedNames, int index)
        {
            string name;
            var matchingGridNode = allGridNodes.FirstOrDefault(node => node.Location.Origin.IsAlmostEqualTo(location));
            if (matchingGridNode != null)
            {
                name = matchingGridNode.Name;
                var i = 1;
                while (usedNames.Contains(name))
                {
                    name = $"{matchingGridNode.Name}({i})";
                    i++;
                }
                usedNames.Add(name);
            }
            else
            {
                name = $"Column-{index}";
            }

            return name;
        }

        #region Override functions

        private static void AttachAllGridLinesOverrides(IEnumerable<ElementProxy<GridLine>> gridLinesProxies, IList<GridlinesOptionsOverride> gridlinesOptionsOverrides, IList<string> enableCreateColumnsGridLinesIds)
        {
            foreach (var gridLine in gridLinesProxies)
            {
                FindGridLineOverride(gridlinesOptionsOverrides, gridLine, enableCreateColumnsGridLinesIds);
            }
        }

        private static GridlinesOptionsOverride FindGridLineOverride(
            IList<GridlinesOptionsOverride> configs,
            ElementProxy<GridLine> gridLineProxy,
            IList<string> enableCreateColumnsGridLinesIds)
        {
            var overrideName = GridlinesOptionsOverride.Name;
            GridlinesOptionsOverride resultConfig = null;

            // See if we already have matching override attached
            var existingOverrideId = gridLineProxy.OverrideIds<GridlinesOptionsOverride>(overrideName).FirstOrDefault();
            if (existingOverrideId != null)
            {
                resultConfig = configs.FirstOrDefault(c => c.Id == existingOverrideId.ToString());
                if (resultConfig != null)
                {
                    return resultConfig;
                }
            }

            resultConfig = configs.FirstOrDefault(config => config.Identity.Name.Equals(gridLineProxy.Element.Name))
                ?? new GridlinesOptionsOverride(
                    Guid.NewGuid().ToString(),
                    new GridlinesOptionsIdentity(gridLineProxy.Element.Name),
                    new GridlinesOptionsValue(
                        enableCreateColumnsGridLinesIds.Contains(gridLineProxy.ElementId.ToString())
                    )
            );
            // Attach the identity and values data to the proxy
            gridLineProxy.AddOverrideIdentity(overrideName, resultConfig.Id, resultConfig.Identity);
            gridLineProxy.AddOverrideValue(overrideName, resultConfig.Value);

            // Make sure configs list has the config in it,
            // in case we call this function more than once on the same function and it's found during step 1 next time
            if (!configs.Contains(resultConfig))
            {
                configs.Add(resultConfig);
            }

            // Make sure proxies list has the proxy so that it will serialize in the model.
            if (!proxies.Contains(gridLineProxy))
            {
                proxies.Add(gridLineProxy);
            }

            return resultConfig;
        }

        #endregion
    }
}