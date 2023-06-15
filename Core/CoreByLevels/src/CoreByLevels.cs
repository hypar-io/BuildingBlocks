using Elements;
using Elements.Geometry;
using System;
using System.Linq;
using System.Collections.Generic;
using GeometryEx;

namespace CoreByLevels
{
    public static class CoreByLevels
    {
        /// <summary>
        /// Creates a building service core including stair enclosures, lift shafts, a mechanical shaft, and restrooms with reference to the quantity and area of the building levels.
        /// </summary>
        /// <param name="model">The input model.</param>
        /// <param name="input">The arguments to the execution.</param>
        /// <returns>A CoreByLevelsOutputs instance containing computed results and the model with any new elements.</returns>
        public static CoreByLevelsOutputs Execute(Dictionary<string, Model> inputModels, CoreByLevelsInputs input)
        {
            var outputs = new CoreByLevelsOutputs();

            var levels = new List<LevelPerimeter>();
            inputModels.TryGetValue("Levels", out var model);
            if (model == null)
            {
                outputs.Errors.Add("The model output named 'Levels' could not be found. Check the upstream functions for errors.");
                return outputs;
            }
            else if (model.AllElementsOfType<LevelPerimeter>().Count() == 0)
            {
                outputs.Errors.Add($"No LevelPerimeters found in the model 'Levels'. Check the output from the function upstream that has a model output 'Levels'.");
                return outputs;
            }
            else if (model.AllElementsOfType<LevelPerimeter>().Count() < 3)
            {
                outputs.Warnings.Add($"The minimum number of LevelPerimeters required is 3.");
                return outputs;
            }

            levels.AddRange(model.AllElementsOfType<LevelPerimeter>());
            var coreMaker = new CoreMaker(levels, input.Setback, input.Rotation);

            if (coreMaker.Perimeter == null)
            {
                outputs.Errors.Add("No valid service core location found. Please check the input 'Setback' and ensure that it will fit within your LevelPerimeter");
                return outputs;
            }

            outputs.Restrooms = coreMaker.Restrooms.Count();
            outputs.Lifts = coreMaker.LiftQuantity;

            outputs.Model.AddElement(new Exclusion(coreMaker.Perimeter, coreMaker.Elevation, Guid.NewGuid(), "Core Exclusion"));
            foreach (var room in coreMaker.Restrooms)
            {
                outputs.Model.AddElement(room);
            }
            foreach (var mech in coreMaker.Mechanicals)
            {
                outputs.Model.AddElement(mech);
            }
            foreach (var stair in coreMaker.Stairs)
            {
                outputs.Model.AddElement(stair);
            }
            foreach (var lift in coreMaker.Lifts)
            {
                outputs.Model.AddElement(lift);
            }
            return outputs;
        }
    }
}