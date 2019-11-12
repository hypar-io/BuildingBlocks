using Elements;
using Elements.Geometry;
using System;
using System.Collections.Generic;
using GeometryEx;

namespace SiteBySketch
{
      public static class SiteBySketch
    {
        /// <summary>
        /// The SiteBySketch function.
        /// </summary>
        /// <param name="model">The input model.</param>
        /// <param name="input">The arguments to the execution.</param>
        /// <returns>A SiteBySketchOutputs instance containing computed results and the model with any new elements.</returns>
        public static SiteBySketchOutputs Execute(Dictionary<string, Model> inputModels, SiteBySketchInputs input)
        {
            var lamina = new Elements.Geometry.Solids.Lamina(input.Perimeter, false);
            var geomRep = new Representation(new List<Elements.Geometry.Solids.SolidOperation>() { lamina });
            var sitMatl = new Material("site", Palette.Emerald, 0.0f, 0.0f);
            var output = new SiteBySketchOutputs(input.Perimeter.Area());
            output.model.AddElement(new Site(input.Perimeter, Guid.NewGuid(), ""));
            output.model.AddElement(new Panel(input.Perimeter, sitMatl, new Transform(), null, Guid.NewGuid(), ""));
            return output;
        }
      }
}