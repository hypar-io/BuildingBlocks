using Elements;
using Elements.Geometry;
using System;
using System.Collections.Generic;
using Elements.Geometry.Solids;

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

            var siteMaterial = new Material("site", "#7ECD9F", 0.0f, 0.0f);
            var output = new SiteBySketchOutputs();
            var sites = new List<Site>();
            var zBump = new Transform(0, 0, 0.001);
            if (input.Perimeter != null)
            {
                var area = input.Perimeter.Area();
                var geomRep = new Lamina(input.Perimeter.TransformedPolygon(zBump), false);
                var site = new Site()
                {
                    Perimeter = input.Perimeter,
                    Area = area,
                    Material = siteMaterial,
                    Representation = geomRep,
                };
                site.AdditionalProperties["Add Id"] = "legacy";
                sites.Add(site);
            }
            var allSites = input.Overrides.Site.CreateElements(
                input.Overrides.Additions.Site,
                input.Overrides.Removals.Site,
                (add) =>
                {
                    var perim = add.Value.Perimeter;
                    var area = Math.Abs(perim.Area());
                    var s = new Site()
                    {
                        Perimeter = add.Value.Perimeter,
                        Area = area,
                        Material = siteMaterial,
                        Representation = new Lamina(perim.TransformedPolygon(zBump), false),
                    };
                    s.AdditionalProperties["Add Id"] = add.Id;
                    return s;
                },
                (site, identity) =>
                {
                    return site.AdditionalProperties["Add Id"] as string == identity.AddId;
                },
                (site, edit) =>
                {
                    site.Perimeter = edit.Value.Perimeter;
                    site.Area = Math.Abs(edit.Value.Perimeter.Area());
                    site.Representation = new Lamina(edit.Value.Perimeter.TransformedPolygon(zBump), false);
                    return site;
                },
                sites);
            output.Model.AddElements(allSites);
            return output;
        }
    }
}