using Elements;
using Elements.Geometry;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.IO;

namespace FloorsByLevels
{
    public static class FloorsByLevels
    {
        private static string _texturePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Textures/Concrete034_1K_Color.png");
        private static string _normalsPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Textures/Concrete034_1K_Normal.png");

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
            if (model == null || model.AllElementsOfType<LevelPerimeter>().Count() == 0)
            {
                throw new ArgumentException("No LevelPerimeters found.");
            }

            var floorMaterial = new Material("Concrete", Colors.White, 0.3, 0.3, _texturePath)
            {
                NormalTexture = _normalsPath
            };

            levels.AddRange(model.AllElementsOfType<LevelPerimeter>());

            var floors = new List<Floor>();
            var floorArea = 0.0;
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
            floors = floors.OrderBy(f => f.Elevation).ToList();
            floors.First().Transform.Move(new Vector3(0.0, 0.0, input.FloorThickness));
            var output = new FloorsByLevelsOutputs(floorArea, floors.Count());
            output.Model.AddElements(floors);
            return output;
        }
    }
}