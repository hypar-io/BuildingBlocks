using System;
using Elements;
using Elements.Geometry;
using System.Collections.Generic;
using GeometryEx;

namespace LevelBySketch
{
      public static class LevelBySketch
    {
        /// <summary>
        /// The LevelBySketch function.
        /// </summary>
        /// <param name="model">The input model.</param>
        /// <param name="input">The arguments to the execution.</param>
        /// <returns>A LevelBySketchOutputs instance containing computed results and the model with any new elements.</returns>
        public static LevelBySketchOutputs Execute(Dictionary<string, Model> inputModels, LevelBySketchInputs input)
        {
            var lamina = new Elements.Geometry.Solids.Lamina(input.Perimeter, false);
            var geomRep = new Representation(new List<Elements.Geometry.Solids.SolidOperation>() { lamina });
            var lvlMatl = new Material("level", Palette.White, 0.0f, 0.0f);
            var output = new LevelBySketchOutputs(input.Perimeter.Area());
            output.model.AddElement(new Level(0.0, Guid.NewGuid(), ""));
            output.model.AddElement(new LevelPerimeter(0.0, input.Perimeter, Guid.NewGuid(), ""));
            output.model.AddElement(new Panel(input.Perimeter, lvlMatl, null, geomRep, Guid.NewGuid(), ""));
            return output;
        }
      }
}