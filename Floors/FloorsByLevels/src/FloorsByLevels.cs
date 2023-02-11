using Elements;
using Elements.Geometry;
using System;
using System.Linq;
using System.Collections.Generic;

namespace FloorsByLevels
{
    public static class FloorsByLevels
    {
        /// <summary>
        /// Generates Floors for each LevelPerimeter in the model configured ith slab thickness and setback..
        /// </summary>
        /// <param name="model">The input model.</param>
        /// <param name="input">The arguments to the execution.</param>
        /// <returns>A FloorsByLevelsOutputs instance containing computed results and the model with any new elements.</returns>
        public static FloorsByLevelsOutputs Execute(Dictionary<string, Model> inputModels, FloorsByLevelsInputs input)
        {
            inputModels.Keys.ToList().ForEach(key => Console.WriteLine(key));

            // Extract LevelVolumes from Levels dependency
            var levels = new List<LevelPerimeter>();
            inputModels.TryGetValue("Levels", out var model);
            var output = new FloorsByLevelsOutputs();
            if (model == null ||
                (
                    model.AllElementsOfType<LevelPerimeter>().Count() == 0 &&
                    model.AllElementsOfType<LevelVolume>().Count() == 0
                )
            )
            {
                output.Errors.Add($"No LevelPerimeters found in the model 'Levels'. Check the output from the function upstream that has a model output 'Levels'.");
                return output;
            }
            var levelVolumes = model.AllElementsOfType<LevelVolume>();
            var floorMaterial = new Material("Concrete", new Color(0.34, 0.34, 0.34, 1.0), 0.3, 0.3);

            levels.AddRange(model.AllElementsOfType<LevelPerimeter>());

            // Extract shafts from Core dependency, if it exists
            var shafts = new List<Polygon>();
            inputModels.TryGetValue("Core", out var core);
            if (core != null)
            {
                var shaftMasses = core.AllElementsOfType<Mass>();
                var shaftPerimeters = shaftMasses.Where(mass => mass.Name == "void").Select(mass => mass.Profile.Perimeter.Project(new Plane(Vector3.Origin, Vector3.ZAxis)));

                shafts.AddRange(shaftPerimeters);
            }

            var floors = new List<Floor>();
            var floorArea = 0.0;
            if (levelVolumes.Count() > 0)
            {
                foreach (var level in levelVolumes)
                {
                    var representation = level.Representation;
                    var profiles = representation.SolidOperations.Count > 1 ?
                        GetProfilesFromRepresentation(representation) :
                        new List<Profile> { level.Profile };
                    foreach (var profile in profiles)
                    {
                        var flrOffsets = profile.Offset(input.FloorSetback * -1);
                        var elevation = level.Transform.Origin.Z;
                        if (flrOffsets.Count() > 0)
                        {
                            if (shafts.Count() > 0)
                            {
                                flrOffsets = flrOffsets.Select(offset => CreateFloorProfile(offset.Perimeter, shafts)).ToList();
                            }

                            foreach (var fo in flrOffsets)
                            {
                                var floor = new Floor(fo, input.FloorThickness,
                                                                new Transform(0.0, 0.0, elevation - input.FloorThickness),
                                                                floorMaterial, null, false, Guid.NewGuid(), null);
                                floor.AdditionalProperties["Level"] = level.Id;
                                floors.Add(floor);
                                floorArea += floor.Area();
                            }
                        }
                        else
                        {
                            var floorProfile = shafts.Count() > 0 ? CreateFloorProfile(profile.Perimeter, shafts.Union(profile.Voids).ToList()) : profile;

                            var floor = new Floor(floorProfile, input.FloorThickness,
                                    new Transform(0.0, 0.0, elevation - input.FloorThickness),
                                    floorMaterial, null, false, Guid.NewGuid(), null);
                            floor.AdditionalProperties["Level"] = level.Id;
                            floors.Add(floor);
                            floorArea += floor.Area();
                        }
                    }
                }
            }
            else
            {
                // Old pathway, which did not support floors with holes
                foreach (var level in levels)
                {
                    Floor floor = null;
                    var flrOffsets = level.Perimeter.Offset(input.FloorSetback * -1);
                    if (flrOffsets.Count() > 0)
                    {
                        floor = new Floor(flrOffsets.First(), input.FloorThickness,
                                new Transform(0.0, 0.0, level.Elevation - input.FloorThickness),
                                floorMaterial, null, false, Guid.NewGuid(), null);
                    }
                    else
                    {
                        floor = new Floor(level.Perimeter, input.FloorThickness,
                                new Transform(0.0, 0.0, level.Elevation - input.FloorThickness),
                                floorMaterial, null, false, Guid.NewGuid(), null);
                    }
                    floors.Add(floor);
                    floorArea += floor.Area();
                }
            }
            floors = floors.OrderBy(f => f.Elevation).ToList();
            floors.First().Transform.Move(new Vector3(0.0, 0.0, input.FloorThickness));
            output.TotalArea = floorArea;
            output.FloorQuantity = floors.Count();
            output.Model.AddElements(floors);
            return output;
        }

        private static List<Profile> GetProfilesFromRepresentation(Representation representation)
        {
            var profiles = new List<Profile>();
            var solids = representation.SolidOperations.Where(so => !so.IsVoid).Select(s => s.Solid);
            foreach (var solid in solids)
            {
                var downFaces = solid.Faces.Where(f => f.Value.Outer.ToPolygon().Normal().Z < -0.99);
                foreach (var face in downFaces)
                {
                    var profile = new Profile(face.Value.Outer.ToPolygon(), face.Value.Inner.Select(i => i.ToPolygon()).ToList());
                    profiles.Add(profile);
                }
            }
            return profiles;
        }

        private static Profile CreateFloorProfile(Polygon perimeter, List<Polygon> openings)
        {
            var initialProfile = new Profile(perimeter, openings);

            try
            {
                var firstOffset = initialProfile.Offset(0.1);

                if (firstOffset.FirstOrDefault() == null)
                {
                    return initialProfile;
                }

                var secondOffset = firstOffset.OrderByDescending(profile => profile.Area()).First().Offset(-0.1);

                if (secondOffset.FirstOrDefault() == null)
                {
                    return initialProfile;
                }

                return secondOffset.OrderByDescending(profile => profile.Area()).First();
            }
            catch
            {
                return initialProfile;
            }

        }
    }
}