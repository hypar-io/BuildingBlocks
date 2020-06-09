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
        /// The CoreBySketch function.
        /// </summary>
        /// <param name="model">The input model.</param>
        /// <param name="input">The arguments to the execution.</param>
        /// <returns>A CoreBySketchOutputs instance containing computed results and the model with any new elements.</returns>
        public static CoreBySketchOutputs Execute(Dictionary<string, Model> inputModels, CoreBySketchInputs input)
        {
            {
                //Extract the Levels from the model.
                var levels = new List<Level>();
                inputModels.TryGetValue("Levels", out var model);
                if (model == null || model.AllElementsOfType<Level>().Count() == 0)
                {
                    throw new ArgumentException("No Levels found.");
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
                var output = new CoreBySketchOutputs(height);
                output.Model.AddElement(svcCore);
                return output;
            }
        }
      }
}