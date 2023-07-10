using Elements;
using Elements.Geometry;
using Elements.Geometry.Solids;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LevelsFromFloors
{
    public static class LevelsFromFloors
    {
        /// <summary>
        /// The LevelsFromFloors function.
        /// </summary>
        /// <param name="model">The input model.</param>
        /// <param name="input">The arguments to the execution.</param>
        /// <returns>A LevelsFromFloorsOutputs instance containing computed results and the model with any new elements.</returns>
        public static LevelsFromFloorsOutputs Execute(Dictionary<string, Model> inputModels, LevelsFromFloorsInputs input)
        {
            var output = new LevelsFromFloorsOutputs();

            var floorsModel = inputModels["Floors"];
            var allFloors = floorsModel.AllElementsOfType<Floor>();
            // for backwards compatibility with old revit exports only producing a working area boundary
            var spaceBoundaries = floorsModel.AllElementsOfType<SpaceBoundary>();
            if (allFloors.Count() == 0)
            {
                if (spaceBoundaries.Count() == 0)
                {
                    var outputError = new LevelsFromFloorsOutputs();
                    outputError.Warnings.Add("No Floors were found in the model.");
                    return outputError;
                }
                else
                {
                    // create dummy floors from space boundaries
                    var dummyFloors = new List<Floor>();
                    foreach (var spaceBoundary in spaceBoundaries)
                    {
                        var floor = new Floor(spaceBoundary.Boundary, 0.1, spaceBoundary.Transform.Moved(0, 0, -0.1));
                        floor.AdditionalProperties["Source Ids"] = new List<Guid> { spaceBoundary.Id };
                        dummyFloors.Add(floor);
                        output.Model.AddElement(floor);
                    }
                    allFloors = dummyFloors;
                }
            }
            foreach (var floor in allFloors)
            {
                if (!floor.AdditionalProperties.ContainsKey("Source Ids"))
                {
                    floor.AdditionalProperties["Source Ids"] = new List<Guid> { floor.Id };
                }
            }

            if (input.FloorMergeTolerance > 0 && allFloors.Count() > 1)
            {
                var topOfSlabElevations = allFloors.Select(f => f.Elevation + f.Thickness).Distinct().OrderBy(l => l).ToList();
                var mergedElevations = new List<double>();
                var floorGroups = new List<List<Floor>>();
                var currElevation = topOfSlabElevations[0];
                var currFloors = new List<Floor>();
                for (int i = 0; i < topOfSlabElevations.Count; i++)
                {
                    var elev = topOfSlabElevations[i];
                    if (elev - currElevation > input.FloorMergeTolerance)
                    {
                        mergedElevations.Add(currElevation);
                        floorGroups.Add(currFloors);
                        currFloors = new List<Floor>();
                        currElevation = elev;
                    }
                    currFloors.AddRange(allFloors.Where(f => f.Elevation + f.Thickness == elev));
                }
                if (currFloors.Count > 0)
                {
                    mergedElevations.Add(currElevation);
                    floorGroups.Add(currFloors);
                }
                var mergedFloors = new List<Floor>();
                // replace the floors with new ones
                foreach (var floorGroup in floorGroups)
                {
                    var bbox3 = floorGroup.GetBBox3();
                    var union = Profile.UnionAll(floorGroup.Select(f => f.ProfileTransformed()));
                    foreach (var u in union)
                    {
                        var newFloor = new Floor(u, bbox3.Max.Z - bbox3.Min.Z, new Transform(0, 0, bbox3.Min.Z), floorGroup.First().Material)
                        {
                            Name = floorGroup.Where(f => f.Name != null).FirstOrDefault()?.Name,
                        };
                        mergedFloors.Add(newFloor);
                        newFloor.AdditionalProperties["Source Ids"] = floorGroup.Select(f => f.Id).ToList();
                        //  output.Model.AddElement(newFloor);
                    }
                }
                allFloors = mergedFloors;
            }
            var uniqueElevations = allFloors.Select(f => f.Elevation + f.Thickness).Distinct().OrderBy(l => l).ToList();
            var levelNameCounter = 1;
            var levels = uniqueElevations.Select(l => new Level(l, Guid.NewGuid(), $"Level {levelNameCounter++}")).ToList();

            var levelGroup = new LevelGroup(levels, Guid.NewGuid(), "Levels From Floors", levels.Max(x => x.Elevation), default, "Levels From Floors Group");

            output.Model.AddElements(levels);
            output.Model.AddElements(levelGroup);

            var levelPerimeters = allFloors.Select(f => new LevelPerimeter(f.Profile.Area(), f.Elevation, f.Profile.Perimeter, Guid.NewGuid(), f.Name));
            output.Model.AddElements(levelPerimeters);
            var floorsOrdered = allFloors.OrderBy(f => f.Elevation).ToList();
            List<LevelVolume> volumes = new List<LevelVolume>();
            List<int> usedFloors = new List<int>();
            var floorCounter = 1;
            for (int i = 0; i < floorsOrdered.Count; i++)
            {
                var currentFloor = floorsOrdered[i];
                for (int j = i + 1; j < floorsOrdered.Count; j++)
                {
                    var nextFloor = floorsOrdered[j];
                    if (Profile.Intersection(new[] { nextFloor.Profile }, new[] { currentFloor.Profile }).Count > 0)
                    {
                        var height = nextFloor.Elevation - (currentFloor.Elevation + currentFloor.Thickness);
                        var extrusion = new Extrude(currentFloor.Profile, height, Vector3.ZAxis, false);
                        var levelVol = new LevelVolume()
                        {
                            Profile = currentFloor.Profile,
                            Height = height,
                            Area = currentFloor.Profile.Area(),
                            Transform = currentFloor.Transform.Concatenated(new Transform(0, 0, currentFloor.Thickness)),
                            Material = BuiltInMaterials.Glass,
                            Representation = extrusion,
                            Name = currentFloor.Name ?? $"Level {floorCounter++}",
                        };
                        levelVol.AdditionalProperties["Floor"] = currentFloor.Id;
                        levelVol.AdditionalProperties["Floors"] = currentFloor.AdditionalProperties["Source Ids"];
                        volumes.Add(levelVol);
                        if (!input.ConstructVolumeAtTopLevel)
                        {
                            usedFloors.Add(j);
                        }
                        usedFloors.Add(i);
                        break;
                    }
                }
            }
            for (int i = 0; i < floorsOrdered.Count; i++)
            {
                if (usedFloors.Contains(i))
                {
                    continue;
                }
                var currentFloor = floorsOrdered[i];
                var height = input.DefaultLevelHeight;
                var extrusion = new Extrude(currentFloor.Profile, height, Vector3.ZAxis, false);
                var levelVol = new LevelVolume()
                {
                    Profile = currentFloor.Profile,
                    Height = height,
                    Area = currentFloor.Profile.Area(),
                    Transform = currentFloor.Transform.Concatenated(new Transform(0, 0, currentFloor.Thickness)),
                    Material = BuiltInMaterials.Glass,
                    Representation = extrusion,
                    Name = currentFloor.Name ?? $"Level {floorCounter++}"
                };
                levelVol.AdditionalProperties["Floor"] = currentFloor.Id;
                levelVol.AdditionalProperties["Floors"] = currentFloor.AdditionalProperties["Source Ids"];
                volumes.Add(levelVol);
            }
            volumes.ForEach((LevelVolume vol) =>
            {
                var bbox = new BBox3(vol);
                // if there's only one volume, drop a little extra to catch image references, etc.
                if (volumes.Count() == 1)
                {
                    bbox.Min = bbox.Min + (0, 0, -0.5);
                }
                // drop the box by a meter to avoid ceilings / beams, etc.
                bbox.Max = bbox.Max + (0, 0, -1);
                var scopeName = vol.Name;
                if (!String.IsNullOrEmpty(vol.BuildingName))
                {
                    scopeName = $"{vol.BuildingName}: {scopeName}";
                }
                var scope = new ViewScope
                {
                    BoundingBox = bbox,
                    Camera = new Camera(default, CameraNamedPosition.Top, CameraProjection.Orthographic),
                    LockRotation = true,
                    Name = scopeName,
                    FunctionVisibility = new Dictionary<string, string> { { "Space Planning", "visible" }, { "Levels", "hidden" } },
                    Actions = new List<object> {
                        new FunctionOverrideAction {
                                Id = $"{scopeName}-spaces-override",
                                Type = "function-override",
                                Function = new FunctionIdentifier {
                                    ModelOutput = "Space Planning Zones"
                                },
                                OverridePaths = new List<string> {
                                    "Spaces"
                                },
                            },
                            new FunctionOverrideAction {
                                Id = $"{scopeName}-circulation-override",
                                Type = "function-override",
                                Function = new FunctionIdentifier {
                                    ModelOutput = "Circulation"
                                },
                                OverridePaths = new List<string> {
                                    "Circulation",
                                    "Corridors"
                                },
                            }
                    }
                };
                vol.AdditionalProperties["Plan View"] = scope;
                output.Model.AddElement(scope);
            });
            output.Model.AddElements(volumes);

            return output;
        }

        private static BBox3 Union(this BBox3 box, BBox3 other)
        {
            return new BBox3(new List<Vector3> { box.Min, box.Max, other.Min, other.Max });
        }

        private static BBox3 GetBBox3(this IEnumerable<Element> elements)
        {
            BBox3 bbox = default;
            foreach (var elem in elements.OfType<GeometricElement>())
            {
                if (bbox == default)
                {
                    bbox = new BBox3(elem);
                }
                else
                {
                    bbox = bbox.Union(new BBox3(elem));
                }
            }
            return bbox;
        }
    }
}