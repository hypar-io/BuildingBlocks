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
        /// Generates a planar Site from a supplied sketch.
        /// </summary>
        /// <param name="inputModels">The input models.</param>
        /// <param name="input">The arguments to the execution.</param>
        /// <returns>A SiteBySketchOutputs instance containing computed results and the model with any new elements.</returns>
        public static SiteBySketchOutputs Execute(Dictionary<string, Model> inputModels, SiteBySketchInputs input)
        {

            var geomRep = new Elements.Geometry.Solids.Lamina(input.Perimeter, false);
            var siteMaterial = new Material("site", Palette.Emerald, 0.0f, 0.0f);
            var area = input.Perimeter.Area();
            var output = new SiteBySketchOutputs(area);
            var site = new Site()
            {
                Perimeter = input.Perimeter,
                Area = area,
                Material = siteMaterial,
                Representation = geomRep,
            };
            output.Model.AddElement(site);
            return output;
        }
    }
}