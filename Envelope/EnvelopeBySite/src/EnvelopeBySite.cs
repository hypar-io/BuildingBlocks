using Elements;
using Elements.Geometry;
using System.Collections.Generic;

namespace EnvelopeBySite
{
      public static class EnvelopeBySite
    {
        /// <summary>
        /// The EnvelopeBySite function.
        /// </summary>
        /// <param name="model">The input model.</param>
        /// <param name="input">The arguments to the execution.</param>
        /// <returns>A EnvelopeBySiteOutputs instance containing computed results and the model with any new elements.</returns>
        public static EnvelopeBySiteOutputs Execute(Dictionary<string, Model> inputModels, EnvelopeBySiteInputs input)
        {
            /// Your code here.
            var height = 1.0;
            var volume = input.Length * input.Width * height;
            var output = new EnvelopeBySiteOutputs(volume);
            var rectangle = Polygon.Rectangle(input.Length, input.Width);
            var mass = new Mass(rectangle, height);
            output.model.AddElement(mass);
            return output;
        }
      }
}