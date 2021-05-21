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
        /// Generates a Level and LevelPerimeters from a sketch and specified elevation.
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
            output.Model.AddElement(new Level(input.LevelElevation, Guid.NewGuid(), ""));
            output.Model.AddElement(new LevelPerimeter(input.Perimeter.Area(), input.LevelElevation, input.Perimeter, Guid.NewGuid(), ""));
            output.Model.AddElement(new Panel(input.Perimeter, 
                                              lvlMatl, 
                                              new Transform(0.0, 0.0, input.LevelElevation), 
                                              geomRep, 
                                              false, 
                                              Guid.NewGuid(), ""));
            return output;
        }
    }
}