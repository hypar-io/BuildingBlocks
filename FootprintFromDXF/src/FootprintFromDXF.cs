using Elements;
using Elements.Geometry;
using System.Collections.Generic;

namespace FootprintFromDXF
{
      public static class FootprintFromDXF
    {
        /// <summary>
        /// The FootprintFromDXF function.
        /// </summary>
        /// <param name="model">The input model.</param>
        /// <param name="input">The arguments to the execution.</param>
        /// <returns>A FootprintFromDXFOutputs instance containing computed results and the model with any new elements.</returns>
        public static FootprintFromDXFOutputs Execute(Dictionary<string, Model> inputModels, FootprintFromDXFInputs input)
        {
            /// Your code here.
            var height = 1.0;
            var volume = input.Length * input.Width * height;
            var output = new FootprintFromDXFOutputs(volume);
            var rectangle = Polygon.Rectangle(input.Length, input.Width);
            var mass = new Mass(rectangle, height);
            output.model.AddElement(mass);
            return output;
        }
      }
}