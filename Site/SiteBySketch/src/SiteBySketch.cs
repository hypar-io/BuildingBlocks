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
        /// <param name="inputModels">The input models.</param>
        /// <param name="input">The arguments to the execution.</param>
        /// <returns>A SiteBySketchOutputs instance containing computed results and the model with any new elements.</returns>
        public static SiteBySketchOutputs Execute(Dictionary<string, Model> inputModels, SiteBySketchInputs input)
        {
            var lamina = new Elements.Geometry.Solids.Lamina(input.Perimeter, false);
            var geomRep = new Representation(new List<Elements.Geometry.Solids.SolidOperation>() { lamina });
            var sitMatl = new Material("site", Palette.Emerald, 0.0f, 0.0f);
            var output = new SiteBySketchOutputs(Math.Abs(input.Perimeter.Area()));
            var site = new Site(input.Perimeter, Math.Abs(input.Perimeter.Area()), new Transform(0, 0, 0), sitMatl, geomRep, false, Guid.NewGuid(), "");
            output.model.AddElement(site);
            return output;
        }
    }
}