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
        /// The FloorsByLevels function.
        /// </summary>
        /// <param name="model">The input model.</param>
        /// <param name="input">The arguments to the execution.</param>
        /// <returns>A FloorsByLevelsOutputs instance containing computed results and the model with any new elements.</returns>
        public static FloorsByLevelsOutputs Execute(Dictionary<string, Model> inputModels, FloorsByLevelsInputs input)
        {
            var levels = new List<LevelPerimeter>();
            inputModels.TryGetValue("Levels", out var model);
            if (model == null || model.AllElementsOfType<LevelPerimeter>().Count() == 0)
            {
                throw new ArgumentException("No LevelPerimeters found.");
            }
            levels.AddRange(model.AllElementsOfType<LevelPerimeter>());
            var floors = new List<Floor>();
            var floorArea = 0.0;
            foreach (var level in levels)
            {
                var floor = new Floor(level.Perimeter, input.FloorThickness,
                            new Transform(0.0, 0.0, level.Elevation - input.FloorThickness),
                            BuiltInMaterials.Concrete, null, Guid.NewGuid(), null);
                floors.Add(floor);
                floorArea += floor.Area();
            }
            floors = floors.OrderBy(f => f.Elevation).ToList();
            var output = new FloorsByLevelsOutputs(floorArea, floors.Count());
            output.model.AddElements(floors);
            return output;
        }
    }
}