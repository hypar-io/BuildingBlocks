using Elements;
using Elements.Geometry;
using System.Collections.Generic;

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
            /// Your code here.
            var height = 1.0;
            var volume = input.Length * input.Width * height;
            var output = new LevelBySketchOutputs(volume);
            var rectangle = Polygon.Rectangle(input.Length, input.Width);
            var mass = new Mass(rectangle, height);
            output.model.AddElement(mass);
            return output;
        }
      }
}