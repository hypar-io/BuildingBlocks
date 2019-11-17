using Elements;
using Elements.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using GeometryEx;

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
            var floors = new List<Floor>();
            inputModels.TryGetValue("Floors", out var model);
            floors.AddRange(model.AllElementsOfType<Floor>());

            foreach (var floor in floors)
            {
                var perimeter = floor.Profile.Perimeter;

            }


            var output = new PlansByFloorsOutputs(0.0, 0.0, 0.0, 0.0, 0.0);
            return output;
        }
    }
}