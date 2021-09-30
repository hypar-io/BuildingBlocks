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
            var levels = new List<LevelPerimeter>();
            inputModels.TryGetValue("Levels", out var model);
            if (model == null ||
                (
                    model.AllElementsOfType<LevelPerimeter>().Count() == 0 &&
                    model.AllElementsOfType<LevelVolume>().Count() == 0
                )
            )
            {
                throw new ArgumentException("No LevelPerimeters found.");
            }
            var levelVolumes = model.AllElementsOfType<LevelVolume>();
            var floorMaterial = new Material("Concrete", new Color(0.34, 0.34, 0.34, 1.0), 0.3, 0.3);

            levels.AddRange(model.AllElementsOfType<LevelPerimeter>());

            var floors = new List<Floor>();
            var floorArea = 0.0;
            if (levelVolumes.Count() > 0)
            {
                foreach (var level in levelVolumes)
                {
                    var flrOffsets = level.Profile.Offset(input.FloorSetback * -1);
                    var elevation = level.Transform.Origin.Z;
                    if (flrOffsets.Count() > 0)
                    {
                        foreach (var fo in flrOffsets)
                        {
                            var floor = new Floor(fo, input.FloorThickness,
                                                            new Transform(0.0, 0.0, elevation - input.FloorThickness),
                                                            floorMaterial, null, false, Guid.NewGuid(), null);
                            floors.Add(floor);
                            floorArea += floor.Area();
                        }
                    }
                    else
                    {
                        var floor = new Floor(level.Profile, input.FloorThickness,
                                new Transform(0.0, 0.0, elevation - input.FloorThickness),
                                floorMaterial, null, false, Guid.NewGuid(), null);
                        floors.Add(floor);
                        floorArea += floor.Area();
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
            var output = new FloorsByLevelsOutputs(floorArea, floors.Count());
            output.Model.AddElements(floors);
            return output;
        }
    }
}