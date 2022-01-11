using Elements;
using Elements.Geometry;
using Elements.Geometry.Solids;
using Elements.Spatial;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FacadeGrid
{
    public static class FacadeGrid
    {
        private static Random random;

        private static Dictionary<string, Panel> globalPanelTypes;

        private static Dictionary<string, FacadeType> globalFacadeTypes;

        private static FacadeGridInputsDisplayMode displayMode;

        private static Dictionary<string, Material> globalMaterials;
        /// <summary>
        /// The FacadeGrid function.
        /// </summary>
        /// <param name="model">The input model.</param>
        /// <param name="input">The arguments to the execution.</param>
        /// <returns>A FacadeGridOutputs instance containing computed results and the model with any new elements.</returns>
        public static FacadeGridOutputs Execute(Dictionary<string, Model> inputModels, FacadeGridInputs input)
        {
            random = new Random(11);
            globalPanelTypes = new Dictionary<string, Panel>();
            globalFacadeTypes = new Dictionary<string, FacadeType>();
            displayMode = input.DisplayMode;
            globalMaterials = new Dictionary<string, Material> {
                {"Irregular", BuiltInMaterials.XAxis},
                {"Rectangular", BuiltInMaterials.YAxis}
            };
            var output = new FacadeGridOutputs();
            var (footprints, hasFootprints) = Retrieve<Footprint>(inputModels, "Masterplan");
            var (envelopes, hasEnvelopes) = Retrieve<Envelope>(inputModels, "Envelope");
            var (grids, hasGrids) = Retrieve<Grid2dElement>(inputModels, "Grid");
            var (levels, hasLevels) = Retrieve<LevelVolume>(inputModels, "Levels");

            if (!hasFootprints && !hasEnvelopes && !hasLevels)
            {
                output.Warnings.Add("This model contains no information from which to build a facade. Consider adding levels, an envelope, or a masterplan");
                return output;
            }
            if (hasFootprints)
            {
                CreateFacadesFromElements(footprints, levels, grids, input, output.Model);
            }
            else if (hasEnvelopes)
            {
                CreateFacadesFromElements(envelopes, levels, grids, input, output.Model);
            }
            else if (hasLevels)
            {
                CreateFacadesFromElements(levels, levels, grids, input, output.Model);
            }
            output.Model.AddElements(globalFacadeTypes.Values);
            // var model = new Model();
            // foreach (var panel in output.Model.AllElementsOfType<Panel>())
            // {
            //     panel.IsElementDefinition = false;
            //     model.AddElement(panel);
            // }
            // output.Model = model;
            return output;
        }

        private static IEnumerable<SolidOperation> GetSolidsFromElements<T>(List<T> elements) where T : GeometricElement
        {
            return elements.SelectMany(fp => fp.Representation.SolidOperations.Select(so =>
            {
                so.LocalTransform = fp.Transform;
                return so;
            }));
        }

        public static void CreateFacadesFromElements<T>(List<T> masses, List<LevelVolume> levels, List<Grid2dElement> grids, FacadeGridInputs input, Model outputModel) where T : GeometricElement
        {
            var xy = new Plane((0, 0), (0, 0, 1));
            foreach (var mass in masses)
            {
                var defaultWidth = 3.0;
                string defaultFacadeType = null;
                var parapetHeight = 0.0;
                if (input.Overrides?.GridDefaults != null)
                {
                    var matchingOverride = input.Overrides.GridDefaults.FirstOrDefault(o => GridDefaultsIdentityMatch(o, mass));
                    if (matchingOverride != null)
                    {
                        defaultWidth = matchingOverride.Value.TypicalPanelWidth;
                        defaultFacadeType = matchingOverride.Value.FacadeTypeName;
                        parapetHeight = matchingOverride.Value.ParapetHeight;
                    }
                }

                var solids = mass.Representation.SolidOperations;
                var elementXForm = mass.Transform;

                var faces = new List<Polygon>();
                foreach (var solid in solids)
                {
                    var vertices = new List<Vector3>();

                    faces.AddRange(solid.Solid.Faces.Select(f =>
                    {
                        var pgon = f.Value.Outer.ToPolygon().TransformedPolygon(mass.Transform);
                        if (pgon.Normal().Z < 0)
                        {
                            return pgon.Reversed();
                        }
                        return pgon;
                    }).Where(p => Math.Abs(p.Normal().Z) > 0.5).Select(p => p.Project(xy)));

                }
                var unions = Profile.Offset(faces.Select(f => new Profile(f)), 1);

                var solidGroups = new List<List<(Solid solid, BBox3 box)>>();
                foreach (var group in unions)
                {
                    var groupSolids = new List<(Solid solid, BBox3 box)>();
                    foreach (var s in solids)
                    {
                        var bbox = new BBox3(s.Solid.Vertices.Select(v => v.Value.Point));
                        if (group.Contains(bbox.Center()))
                        {
                            groupSolids.Add((s.Solid, bbox));
                        }
                    }
                    solidGroups.Add(groupSolids);
                }

                foreach (var solidGrp in solidGroups)
                {
                    var solidBbox = new BBox3(solidGrp.SelectMany(grp => grp.solid.Vertices.Select(v => elementXForm.OfPoint(v.Value.Point))));
                    var levelsInSolid = levels.Where(l => solidBbox.Contains(l.Transform.OfPoint(l.Profile.Perimeter.PointInternal()) + (0, 0, 0.01)));
                    var horizontalFaces = solidGrp
                      .SelectMany(s => s.solid.Faces.Values)
                      .Where(f => Math.Abs(f.Outer.ToPolygon().Normal().Dot(Vector3.ZAxis)) < 0.7);
                    var levelElevations = levelsInSolid.Select(lv => lv.Transform.Origin.Z).Distinct();
                    var mergedFaces = MergeFaces(horizontalFaces);
                    foreach (var face in mergedFaces)
                    {
                        var outer = face.Perimeter.TransformedPolygon(elementXForm);
                        var normal = outer.Normal();
                        var horizontalVector = Vector3.ZAxis.Cross(normal).Unitized();
                        var upVector = normal.Cross(horizontalVector);
                        // TODO — handle non-vertical faces by projecting the height vector onto the up vector
                        var heightVector = new Vector3(0, 0, parapetHeight);
                        if (parapetHeight > 0)
                        {
                            var topEdges = outer.Segments().Where(s => s.Direction().Dot(horizontalVector) < -0.7).ToList();
                            var transformedTopEdges = topEdges.Select(e => e.TransformedLine(new Transform(heightVector))).ToList();
                            var newVertices = new List<Vector3>();
                            foreach (var v in outer.Vertices)
                            {
                                var topEdgeStartMatch = topEdges.FindIndex(te => te.Start == v);
                                var topEdgeEndMatch = topEdges.FindIndex(te => te.End == v);
                                if (topEdgeStartMatch >= 0)
                                {
                                    newVertices.Add(transformedTopEdges[topEdgeStartMatch].Start);
                                }
                                else if (topEdgeEndMatch >= 0)
                                {
                                    newVertices.Add(transformedTopEdges[topEdgeEndMatch].End);
                                }
                                else
                                {
                                    newVertices.Add(v);
                                }
                            }
                            outer = new Polygon(newVertices);
                        }
                        var polygons = new List<Polygon> { outer };
                        var inner = face.Voids?.Select(i => i.TransformedPolygon(elementXForm)).ToList();
                        if (inner == null)
                        {
                            inner = new List<Polygon>();
                        }
                        polygons.AddRange(inner);
                        var profile = new Profile(outer, inner);
                        var massFace = new MassFace(profile, mass.Id);
                        var massFaceSection = new MassFaceSection(profile, massFace.Id)
                        {
                            FacadeTypeName = defaultFacadeType ?? "Primary"
                        };
                        massFaceSection.GenerateOverrideIdentityProperties(solidBbox.Center());
                        TryFindOverrideMatchAndApply(massFaceSection, input);
                        // TODO - split mass faces into multiple sections with overrides, and nest the below
                        outputModel.AddElements(massFace, massFaceSection);
                        var transform = new Transform(outer.Vertices.OrderBy(v => v.Z).First(), horizontalVector, normal);
                        var inverse = transform.Inverted();
                        var grid2d = new Grid2d(polygons, transform);

                        if (massFaceSection.Grid == null)
                        {
                            List<Vector3> points = GetLevelPoints(levelElevations, outer, normal, horizontalVector);
                            if (parapetHeight > 0)
                            {
                                points.Add(new Vector3(0, 0, solidBbox.Max.Z));
                            }
                            massFaceSection.Grid = CreateAndApplyDefaultGridSettings(points, grid2d, transform, defaultWidth);
                        }
                        else
                        {
                            ApplyGridSettings(grid2d, massFaceSection.Grid);
                        }
                        var uGrid = grid2d.U;

                        foreach (var cell in uGrid.Cells ?? new List<Grid1d>())
                        {
                            cell.Type = $"{cell.Domain.Length:0.00}";
                        }
                        foreach (var cell in grid2d.V.GetCells())
                        {
                            cell.Type = $"{cell.Domain.Length:0.00}";
                        }
                        InstancesFromGrid2d(outputModel, mass, massFace, massFaceSection, grid2d);
                    }
                }
            }
        }

        private static bool GridDefaultsIdentityMatch<T>(GridDefaultsOverride o, T mass) where T : GeometricElement
        {
            if (mass is Footprint f)
            {
                if (f.AdditionalProperties.TryGetValue("Building Name", out var buildingName))
                {
                    return (string)buildingName == o.Identity.BuildingName;
                }
                else if (o.Identity.Boundary.Contains(f.Boundary.Centroid()))
                {
                    return true;
                }
            }
            return false;
        }

        private static List<Vector3> GetLevelPoints(IEnumerable<double> levelElevations, Polygon outer, Vector3 normal, Vector3 horizontalVector)
        {
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

            return points;
        }

        private static void ApplyGridSettings(Grid2d grid2d, Grid2dInput grid)
        {
            if (grid?.U?.SplitPoints?.Count > 0)
            {
                grid2d.U.SplitAtPoints(grid.U.SplitPoints);
            }
            if (grid?.V?.SplitPoints?.Count > 0)
            {
                grid2d.V.SplitAtPoints(grid.V.SplitPoints);
            }
        }

        private static Grid2dInput CreateAndApplyDefaultGridSettings(List<Vector3> points, Grid2d grid2d, Transform transform, double defaultWidth)
        {

            grid2d.V.SplitAtPoints(points);
            var uGrid = grid2d.U;
            uGrid.DivideByFixedLength(defaultWidth, FixedDivisionMode.RemainderAtBothEnds);

            return new Grid2dInput
            (
                new Grid1dInput(
                    (grid2d.U.Curve as Line).ToPolyline(1).TransformedPolyline(transform),
                    new List<Vector3>(),
                    Grid1dInputSubdivisionMode.Divide_by_fixed_length,
                    new SubdivisionSettings(
                        1,
                        defaultWidth,
                        defaultWidth,
                        SubdivisionSettingsRemainderMode.Remainder_at_both_ends,
                        SubdivisionSettingsCycleMode.Wrap
                    )
                ),
                new Grid1dInput
                (
                    (grid2d.V.Curve as Line).ToPolyline(1).TransformedPolyline(transform),
                    grid2d.V.GetCellSeparators().Select(v => transform.OfPoint(v)).ToList(),
                    Grid1dInputSubdivisionMode.Manual,
                    new SubdivisionSettings(
                        1,
                        4,
                        4,
                        SubdivisionSettingsRemainderMode.Remainder_at_end,
                        SubdivisionSettingsCycleMode.Wrap
                    )
                )
            );
        }

        private static void TryFindOverrideMatchAndApply(MassFaceSection massFaceSection, FacadeGridInputs input)
        {
            if (input.Overrides?.Grids != null)
            {
                var matchScore = double.MaxValue;
                GridsOverride matchingOverride = null;
                foreach (var gridOverride in input.Overrides.Grids)
                {
                    // prefer overrides that point in the same direction
                    var normalScore = 1 - gridOverride.Identity.Normal.Dot(massFaceSection.Normal);
                    // prefer overrides whose offset from their base mass is roughly the same
                    var offsetScore = (massFaceSection.Centroid - massFaceSection.ElementCentroid).DistanceTo(gridOverride.Identity.Centroid - gridOverride.Identity.ElementCentroid);
                    if (matchScore > normalScore + offsetScore)
                    {
                        matchScore = normalScore + offsetScore;
                        matchingOverride = gridOverride;
                    }
                }
                if (matchScore < 3.0)
                {
                    if (matchingOverride.Value.Grid?.U?.SplitPoints != null &&
                        matchingOverride.Value.Grid.U.SplitPoints.Count > 0 &&
                        matchingOverride.Value.Grid?.V?.SplitPoints != null &&
                        matchingOverride.Value.Grid.V.SplitPoints.Count > 0)
                    {
                        massFaceSection.Grid = matchingOverride.Value.Grid;
                    }
                    massFaceSection.FacadeTypeName = matchingOverride.Value.FacadeTypeName ?? massFaceSection.FacadeTypeName;
                    Identity.AddOverrideIdentity(massFaceSection, matchingOverride);
                }
            }
            if (!globalFacadeTypes.ContainsKey(massFaceSection.FacadeTypeName))
            {
                globalFacadeTypes.Add(massFaceSection.FacadeTypeName, new FacadeType { Name = massFaceSection.FacadeTypeName });
            }
            massFaceSection.FacadeType = globalFacadeTypes[massFaceSection.FacadeTypeName].Id;
        }

        private static List<Profile> MergeFaces(IEnumerable<Face> horizontalFaces)
        {
            var profiles = new List<Profile>();
            var faceDirections = horizontalFaces.Select((face) =>
            {
                var faceBoundary = face.Outer.ToPolygon();
                var normal = faceBoundary.Normal();
                var centroid = faceBoundary.Centroid();
                var positionAlongNormal = centroid.Dot(normal);
                return ((Face face, Vector3 normal, Vector3 centroid, double position))(face, normal, centroid, positionAlongNormal);
            });
            var possibleKeys = new Dictionary<(double nx, double ny, double nz, double o), List<(Face face, Vector3 normal, Vector3 centroid, double position)>>();
            foreach (var faceDirection in faceDirections)
            {
                (double nx, double ny, double nz, double o) key = (faceDirection.normal.X, faceDirection.normal.Y, faceDirection.normal.Z, faceDirection.position);
                var keyMatch = possibleKeys.Keys.FirstOrDefault((k) =>
                {
                    return
                      Math.Abs(k.nx - key.nx) < 0.01 &&
                      Math.Abs(k.ny - key.ny) < 0.01 &&
                      Math.Abs(k.nz - key.nz) < 0.01 &&
                      Math.Abs(k.o - key.o) < 0.01;
                });
                if (keyMatch == default)
                {
                    possibleKeys.Add(key, new List<(Face face, Vector3 normal, Vector3 centroid, double position)> { faceDirection });
                }
                else
                {
                    possibleKeys[keyMatch].Add(faceDirection);
                }
            }
            foreach (var group in possibleKeys)
            {
                var groupNormal = group.Value.Select(v => v.normal).Average().Unitized();
                var groupCenter = group.Value.Select(v => v.centroid).Average();
                var transform = new Transform(groupCenter, groupNormal);
                var inverse = transform.Inverted();
                var groupProfiles = group.Value.Select(g =>
                  new Profile(
                    g.face.Outer.ToPolygon().TransformedPolygon(inverse),
                    g.face.Inner?.Select(i => i.ToPolygon().TransformedPolygon(inverse))?.ToList() ?? new List<Polygon>()
                    )
                );
                var offsets = Profile.Offset(groupProfiles, 0.1);
                var offsetsIn = Profile.Offset(offsets, -0.1);
                profiles.AddRange(offsetsIn.Select(p => p.Transformed(transform)));
            }
            return profiles;
        }

        public static (List<T> elements, bool hasModel) Retrieve<T>(Dictionary<string, Model> inputModels, string modelName)
        {
            if (inputModels.TryGetValue(modelName, out Model model))
            {
                return (model.AllElementsOfType<T>().ToList(), true);
            }
            return (new List<T>(), false);
        }

        private static readonly Material gridCurveMaterial = new Material("GridDisplay") { Color = Colors.Gray };
        private static void InstancesFromGrid2d(Model outputModel, Element envelopeElem, MassFace massFace, MassFaceSection massFaceSection, Grid2d grid2d)
        {
            var cells = grid2d.GetCells();
            foreach (var cell in cells)
            {
                var panelBoundaryInSpace = cell.GetCellGeometry() as Polygon;
                var normal = panelBoundaryInSpace.Normal();
                var cellType = cell.Type;
                cellType = $"{massFaceSection.FacadeTypeName ?? "Primary"} - {cellType}";
                if (cell.IsTrimmed())
                {
                    var type = $"{cellType}-irreg";
                    var trimmedGeo = cell.GetTrimmedCellGeometry().OfType<Polygon>().ToList();
                    if (trimmedGeo.Count == 0)
                    {
                        continue;
                    }
                    if (trimmedGeo.Sum(g => g.Area()) < (cell.GetCellGeometry() as Polygon).Area() * 0.999)
                    {
                        var profile = new Profile(trimmedGeo);
                        var horizontalVector = normal.Dot(Vector3.ZAxis) > 0.99 ? Vector3.XAxis : Vector3.ZAxis.Cross(normal).Unitized();
                        var origin = profile.Perimeter.Vertices.OrderBy(v => v.Z).First();
                        var transform = new Transform(origin, horizontalVector, normal);
                        var inverse = transform.Inverted();
                        var cellColorMaterial = MaterialForCellType(type);
                        var mcs = profile.ToModelCurves(material: gridCurveMaterial);
                        foreach (var mc in mcs)
                        {
                            mc.SetSelectable(false);
                        }
                        outputModel.AddElements(mcs);
                        var panel = new Panel(profile.Perimeter.TransformedPolygon(inverse))
                        {
                            Material = cellColorMaterial,
                            Representation = new Lamina(profile),
                            IsElementDefinition = true,
                            Name = type
                        };
                        var instance = panel.CreateInstance(transform, type);
                        instance.AdditionalProperties["Envelope"] = envelopeElem.Id;
                        instance.AdditionalProperties["Face"] = massFace.Id;
                        instance.AdditionalProperties["Face Section"] = massFaceSection.Id;
                        instance.AdditionalProperties["Facade Type"] = massFaceSection.FacadeType;
                        outputModel.AddElement(instance);
                        continue;
                    }
                }
                if (!globalPanelTypes.ContainsKey(cellType))
                {
                    var cellBoundary = new Polygon(new Vector3[] {
                                new Vector3(0,0),
                                new Vector3(cell.U.Domain.Length, 0, 0),
                                new Vector3(cell.U.Domain.Length, cell.V.Domain.Length, 0),
                                new Vector3(0, cell.V.Domain.Length, 0),
                            });
                    var cellColorMaterial = MaterialForCellType(cellType);
                    var panel = new Panel(cellBoundary, cellColorMaterial, new Transform(), null, true, Guid.NewGuid(), cellType);
                    globalPanelTypes.Add(cellType, panel);
                }

                var panelBottomEdge = panelBoundaryInSpace.Segments()[0];
                var panelTransform = new Transform(panelBottomEdge.Start, panelBottomEdge.Direction(), normal);
                var panelInstance = globalPanelTypes[cellType].CreateInstance(panelTransform, cellType);
                panelInstance.AdditionalProperties["Envelope"] = envelopeElem.Id;
                panelInstance.AdditionalProperties["Face"] = massFace.Id;
                panelInstance.AdditionalProperties["Face Section"] = massFaceSection.Id;
                panelInstance.AdditionalProperties["Facade Type"] = massFaceSection.FacadeType;
                var boundary = new ModelCurve(panelBoundaryInSpace)
                {
                    Transform = new Transform(normal * 0.01),
                    Name = cellType,
                    Material = gridCurveMaterial
                };
                boundary.SetSelectable(false);
                outputModel.AddElement(panelInstance);
                outputModel.AddElement(boundary);
            }
        }

        private static Material MaterialForCellType(string type)
        {
            var fragments = type.Split('-').Select(t => t.Trim());
            var key = displayMode switch
            {
                FacadeGridInputsDisplayMode.By_Size => fragments.Last(),
                FacadeGridInputsDisplayMode.By_Type => fragments.First(),
                FacadeGridInputsDisplayMode.Highlight_Irregular => type.Contains("irreg") ? "Irregular" : "Rectangular",
                _ => type,
            };

            if (!globalMaterials.ContainsKey(key) || (key.Contains("irreg") && displayMode != FacadeGridInputsDisplayMode.Highlight_Irregular))
            {
                // we're overwriting the irreg values each time, but that's ok.
                globalMaterials[key] = new Material(type, random.NextColor(), 0.0, 0.0, null, true, false);
            }
            return globalMaterials[key];
        }

    }
}