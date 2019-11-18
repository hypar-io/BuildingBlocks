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
            var levels = new List<Level>();
            inputModels.TryGetValue("Levels", out var model);
            levels.AddRange(model.AllElementsOfType<Level>());

            var floors = new List<Floor>();
            var floorArea = 0.0;

            foreach (var level in levels)
            {
                floors.Add(new Floor(level.Perimeter, input.FloorThickness, 0.0,
                           new Transform(0.0, 0.0, level.Elevation - input.FloorThickness),
                           BuiltInMaterials.Concrete, null, Guid.NewGuid(), null));
                floorArea += level.Perimeter.Area();
            }

            floors = floors.OrderBy(f => f.Elevation).ToList();
            var output = new FloorsByLevelsOutputs(floorArea, floors.Count());
            output.model.AddElements(floors);
            return output;
        }
    }
}