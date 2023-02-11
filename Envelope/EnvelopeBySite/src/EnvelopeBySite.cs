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
        /// Generates a building Envelope from a Site boundary.
        /// </summary>
        /// <param name="model">The input model.</param>
        /// <param name="input">The arguments to the execution.</param>
        /// <returns>A EnvelopeBySiteOutputs instance containing computed results and the model with any new elements.</returns>
        public static EnvelopeBySiteOutputs Execute(Dictionary<string, Model> inputModels, EnvelopeBySiteInputs input)
        {
            var output = new EnvelopeBySiteOutputs(input.BuildingHeight, input.FoundationDepth);
            // Retrieve site information from incoming models.
            var sites = new List<Site>();
            inputModels.TryGetValue("Site", out var model);
            if (model == null)
            {
                output.Errors.Add("The model output named 'Site' could not be found. Check the upstream functions for errors.");
                return output;
            }
            sites.AddRange(model.AllElementsOfType<Site>());
            sites = sites.OrderByDescending(e => e.Perimeter.Area()).ToList();

            // Set input values based on whether we should consider setbacks
            var siteSetback = input.SiteSetback;
            var setbackInterval = input.UseSetbacks ? input.SetbackInterval : 0;
            var setbackDepth = input.UseSetbacks ? input.SetbackDepth : 0;

            var xy = new Plane((0, 0, 0), (0, 0, 1));

            foreach (var site in sites)
            {
                var siteCentroid = site.Perimeter.Centroid();
                var overridesForSite = input.Overrides?.EnvelopeFootprint?.Where(o => site.Perimeter.Contains(o.Identity.SiteCentroid)) ?? new List<EnvelopeFootprintOverride>();
                var perims = site.Perimeter.Offset(siteSetback * -1);
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
                var envelopes = new List<Envelope>();

                // Create the foundation Envelope.
                var fndElevation = input.FoundationDepth * -1;
                var matchingFndOverride = overridesForSite.FirstOrDefault(o => o.Identity.Elevation.ApproximatelyEquals(fndElevation, 1));
                var fndPerimeter = matchingFndOverride?.Value?.Perimeter?.Project(xy) ?? perimeter;
                var extrude = new Elements.Geometry.Solids.Extrude(perimeter, input.FoundationDepth, Vector3.ZAxis, false);
                var geomRep = new Representation(new List<Elements.Geometry.Solids.SolidOperation>() { extrude });
                var fndMatl = new Material("foundation", Palette.Gray, 0.0f, 0.0f);
                var envMatl = new Material("envelope", Palette.Aqua, 0.0f, 0.0f);
                var fndXform = new Transform(0.0, 0.0, input.FoundationDepth * -1);
                var fndEnvelope = new Envelope(fndPerimeter, fndElevation, input.FoundationDepth, Vector3.ZAxis,
                                                 0.0, fndXform, fndMatl, geomRep, false, Guid.NewGuid(), "")
                {
                    Perimeter = fndPerimeter.TransformedPolygon(fndXform),
                    SiteCentroid = siteCentroid
                };
                if (matchingFndOverride != null)
                {
                    Identity.AddOverrideIdentity(fndEnvelope, matchingFndOverride);
                }
                envelopes.Add(fndEnvelope);


                // Create the Envelope at the location's zero plane.
                var matchingZeroOverride = overridesForSite.FirstOrDefault(o => o.Identity.Elevation.ApproximatelyEquals(0, 1));
                var zeroPerimeter = matchingZeroOverride?.Value?.Perimeter?.Project(xy) ?? perimeter;
                var tiers = setbackInterval == 0 ? 0 : Math.Floor(input.BuildingHeight / setbackInterval) - 1;
                var tierHeight = tiers > 0 ? input.BuildingHeight / (tiers + 1) : input.BuildingHeight;
                extrude = new Elements.Geometry.Solids.Extrude(zeroPerimeter, tierHeight, Vector3.ZAxis, false);
                geomRep = new Representation(new List<Elements.Geometry.Solids.SolidOperation>() { extrude });
                var zeroEnvelope = new Envelope(zeroPerimeter, 0.0, tierHeight, Vector3.ZAxis, 0.0,
                              new Transform(), envMatl, geomRep, false, Guid.NewGuid(), "")
                {
                    Perimeter = zeroPerimeter,
                    SiteCentroid = siteCentroid
                };
                if (matchingZeroOverride != null)
                {
                    Identity.AddOverrideIdentity(zeroEnvelope, matchingZeroOverride);
                    perimeter = zeroPerimeter; // this way tiers above, if not overridden, respect the ground floor boundary.
                }
                envelopes.Add(zeroEnvelope);

                // Create the remaining Envelope Elements.
                var offsFactor = -1;
                var elevFactor = 1;
                var totalHeight = 0.0;

                for (int i = 0; i < tiers; i++)
                {
                    if (totalHeight + tierHeight > input.BuildingHeight)
                    {
                        break;
                    }
                    var tierElev = tierHeight * elevFactor;

                    var tryPer = perimeter.Offset(setbackDepth * offsFactor);
                    if (tryPer.Count() == 0 || tryPer.First().Area() < input.MinimumTierArea)
                    {
                        break;
                    }


                    tryPer = tryPer.OrderByDescending(p => p.Area()).ToArray();

                    var matchingTierOverride = overridesForSite.FirstOrDefault(o => o.Identity.Elevation.ApproximatelyEquals(tierElev, 1));
                    var tierPerimeter = matchingTierOverride?.Value?.Perimeter?.Project(xy) ?? tryPer.First();

                    extrude = new Elements.Geometry.Solids.Extrude(tierPerimeter, tierHeight, Vector3.ZAxis, false);
                    geomRep = new Representation(new List<Elements.Geometry.Solids.SolidOperation>() { extrude });
                    var elevationXform = new Transform(0.0, 0.0, tierElev);
                    var tierEnvelope = new Envelope(tierPerimeter, tierElev, tierHeight, Vector3.ZAxis, 0.0,
                                  elevationXform, envMatl, geomRep, false, Guid.NewGuid(), "")
                    {
                        Perimeter = tierPerimeter.TransformedPolygon(elevationXform),
                        SiteCentroid = siteCentroid
                    };
                    if (matchingTierOverride != null)
                    {
                        Identity.AddOverrideIdentity(tierEnvelope, matchingTierOverride);
                    }
                    envelopes.Add(tierEnvelope);

                    offsFactor--;
                    elevFactor++;
                    totalHeight = totalHeight + tierHeight;
                }
                envelopes.OrderBy(e => e.Elevation).ToList().ForEach(e => output.Model.AddElement(e));
            }
            return output;
        }
    }
}