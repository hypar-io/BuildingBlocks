using Elements;
using Elements.Geometry;
using System;
using System.Linq;
using System.Collections.Generic;

namespace CoreBySketch
{
    public static class CoreBySketch
    {
        /// <summary>
        /// Creates a building service core enclosure from a sketch and referenced Levels.
        /// </summary>
        /// <param name="model">The input model.</param>
        /// <param name="input">The arguments to the execution.</param>
        /// <returns>A CoreBySketchOutputs instance containing computed results and the model with any new elements.</returns>
        public static CoreBySketchOutputs Execute(Dictionary<string, Model> inputModels, CoreBySketchInputs input)
        {
            {
                var output = new CoreBySketchOutputs();
                //Extract the Levels from the model.
                var levels = new List<Level>();
                inputModels.TryGetValue("Levels", out var model);
                if (model == null)
                {
                    output.Errors.Add("The model output named 'Levels' could not be found. Check the upstream functions for errors.");
                    return output;
                }
                else if (model.AllElementsOfType<Level>().Count() == 0)
                {
                    output.Errors.Add($"No Levels found in the model 'Levels'. Check the output from the function upstream that has a model output 'Levels'.");
                    return output;
                }
                levels.AddRange(model.AllElementsOfType<Level>());
                var top = levels.OrderByDescending(l => l.Elevation).First().Elevation + input.CoreHeightAboveRoof;
                var elevation = levels.OrderBy(l => l.Elevation).First().Elevation;
                var height = top - elevation;
                // Create the Core extrusion.
                var extrude = new Elements.Geometry.Solids.Extrude(input.Perimeter, height, Vector3.ZAxis, false);
                var geomRep = new Representation(new List<Elements.Geometry.Solids.SolidOperation>() { extrude });
                var corMatl = new Material("core", new Color(1.0, 1.0, 1.0, 0.6), 0.0f, 0.0f);
                var svcCore = new ServiceCore(input.Perimeter, Vector3.ZAxis, elevation, height, 0.0, new Transform(0.0, 0.0, elevation), corMatl, geomRep, false, Guid.NewGuid(), "serviceCore");
                output.Height = height;
                output.Model.AddElement(svcCore);
                return output;
            }
        }
    }
}