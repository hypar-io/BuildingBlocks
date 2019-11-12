using Elements;
using Elements.Geometry;
using System.Collections.Generic;
using System;
using System.Linq;
using GeometryEx;

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
            // Retrieve site information from incoming models.
            var sites = new List<Site>();
            inputModels.TryGetValue("Site", out var model);
            sites.AddRange(model.AllElementsOfType<Site>());
            sites = sites.OrderByDescending(e => e.Perimeter.Area()).ToList();
            var output = new EnvelopeBySiteOutputs(input.BuildingHeight, input.FoundationDepth);

            foreach (var site in sites)
            {
                var perims = site.Perimeter.Offset(input.SiteSetback * -1);
                if (perims.Count() == 0)
                {
                    continue;
                }
                perims = perims.OrderByDescending(p => p.Area()).ToArray();
                var perimeter = perims.First();
                if (perimeter.Area() < input.MinimumTierArea)
                {
                    continue;
                }

                // Create the foundation Envelope.
                var extrude = new Elements.Geometry.Solids.Extrude(perimeter, input.FoundationDepth, Vector3.ZAxis, 0.0, false);
                var geomRep = new Representation(new List<Elements.Geometry.Solids.SolidOperation>() { extrude });
                var fndMatl = new Material("foundation", Palette.Gray, 0.0f, 0.0f);
                var envMatl = new Material("envelope", Palette.Aqua, 0.0f, 0.0f);
                var envelope = new List<Envelope>()
                {
                    new Envelope(perimeter, input.FoundationDepth * -1, input.FoundationDepth, Vector3.ZAxis,
                                 0.0, new Transform(0.0, 0.0, input.FoundationDepth * -1), fndMatl, geomRep, Guid.NewGuid(), "")
                };

                // Create the Envelope at the location's zero plane.
                var tiers = Math.Floor(input.BuildingHeight / input.SetbackInterval);
                var tierHeight = tiers > 0 ? input.BuildingHeight / tiers : input.BuildingHeight;
                extrude = new Elements.Geometry.Solids.Extrude(perimeter, tierHeight, Vector3.ZAxis, 0.0, false);
                geomRep = new Representation(new List<Elements.Geometry.Solids.SolidOperation>() { extrude });
                envelope.Add(new Envelope(perimeter, 0.0, tierHeight, Vector3.ZAxis, 0.0,
                             new Transform(), envMatl, geomRep, Guid.NewGuid(), ""));

                // Create the remaining Envelope Elements.
                var offsFactor = -1;
                var elevFactor = 1;
                for (int i = 0; i < tiers; i++)
                {
                    var tryPer = perimeter.Offset(input.SetbackDepth * offsFactor);
                    if (tryPer.Count() == 0 || tryPer.First().Area() < input.MinimumTierArea)
                    {
                        break;
                    }
                    tryPer = tryPer.OrderByDescending(p => p.Area()).ToArray();
                    extrude = new Elements.Geometry.Solids.Extrude(tryPer.First(), tierHeight, Vector3.ZAxis, 0.0, false);
                    geomRep = new Representation(new List<Elements.Geometry.Solids.SolidOperation>() { extrude });
                    envelope.Add(new Envelope(tryPer.First(), tierHeight * elevFactor, tierHeight, Vector3.ZAxis, 0.0,
                                new Transform(0.0, 0.0, tierHeight * elevFactor), envMatl, geomRep, Guid.NewGuid(), ""));
                    offsFactor--;
                    elevFactor++;
                }
                foreach (var item in envelope.OrderBy(e => e.Elevation))
                {
                    output.model.AddElement(item);
                }
            }
            return output;
        }
    }
}