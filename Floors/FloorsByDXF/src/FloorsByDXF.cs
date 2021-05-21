using Elements;
using Elements.Geometry;
using System.Collections.Generic;

namespace FloorsByDXF
{
      public static class FloorsByDXF
    {
        /// <summary>
        /// Generates the specified quantity of stacked Floors from a DXF LWPolyline pattern and supplied elevations.
        /// </summary>
        /// <param name="model">The input model.</param>
        /// <param name="input">The arguments to the execution.</param>
        /// <returns>A FloorsByDXFOutputs instance containing computed results and the model with any new elements.</returns>
        public static FloorsByDXFOutputs Execute(Dictionary<string, Model> inputModels, FloorsByDXFInputs input)
        {
            /// Your code here.
            var height = 1.0;
            var volume = input.Length * input.Width * height;
            var output = new FloorsByDXFOutputs(volume);
            var rectangle = Polygon.Rectangle(input.Length, input.Width);
            var mass = new Mass(rectangle, height);
            output.Model.AddElement(mass);
            return output;
        }
      }
}