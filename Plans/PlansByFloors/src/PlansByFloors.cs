using Elements;
using Elements.Geometry;
using System.Collections.Generic;

namespace PlansByFloors
{
      public static class PlansByFloors
    {
        /// <summary>
        /// The PlansByFloors function.
        /// </summary>
        /// <param name="model">The input model.</param>
        /// <param name="input">The arguments to the execution.</param>
        /// <returns>A PlansByFloorsOutputs instance containing computed results and the model with any new elements.</returns>
        public static PlansByFloorsOutputs Execute(Dictionary<string, Model> inputModels, PlansByFloorsInputs input)
        {
            /// Your code here.
            var height = 1.0;
            var volume = input.Length * input.Width * height;
            var output = new PlansByFloorsOutputs(volume);
            var rectangle = Polygon.Rectangle(input.Length, input.Width);
            var mass = new Mass(rectangle, height);
            output.model.AddElement(mass);
            return output;
        }
      }
}