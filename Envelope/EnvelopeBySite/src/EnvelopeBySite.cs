using Elements;
using Elements.Geometry;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Reflection.Metadata;

namespace EnvelopeBySite
{
    public static class EnvelopeBySite
    {
        private static readonly Plane XY_PLANE = new((0, 0, 0), (0, 0, 1));

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

            var zeroElevationEnvelopes = CreateZeroElevationEnvelopes(sites, input);
            var foundationEnvelopes = zeroElevationEnvelopes.Select(envelope => CreateFoundationEnvelope(envelope, input)).ToList();
            foundationEnvelopes = input.Overrides.EnvelopeFootprint.Apply(
                foundationEnvelopes,
                (envelope, identity) => IsMatchingIdentity(envelope, identity),
                (envelope, edit) => ModifyEnvelopeFootprint(envelope, edit)
            );
            output.Model.AddElements(foundationEnvelopes);

            zeroElevationEnvelopes = input.Overrides.EnvelopeFootprint.Apply(
                zeroElevationEnvelopes,
                (envelope, identity) => IsMatchingIdentity(envelope, identity),
                (envelope, edit) => ModifyEnvelopeFootprint(envelope, edit)
            );
            output.Model.AddElements(zeroElevationEnvelopes);

            var tierEnvelopes = zeroElevationEnvelopes.SelectMany(envelope => CreateTierEnvelopes(envelope, input)).ToList();
            tierEnvelopes = input.Overrides.EnvelopeFootprint.Apply(
                tierEnvelopes,
                (envelope, identity) => IsMatchingIdentity(envelope, identity),
                (envelope, edit) => ModifyEnvelopeFootprint(envelope, edit)
            );
            output.Model.AddElements(tierEnvelopes.OrderBy(e => e.Elevation).ToList());
            
            return output;
        }

        private static List<Envelope> CreateZeroElevationEnvelopes(List<Site> sites, EnvelopeBySiteInputs input)
        {
            var envelopes = new List<Envelope>();

            foreach (var site in sites)
            {
                var perimeter = GetSiteZeroElevationPolygon(site, input.SiteSetback, input.MinimumTierArea);

                if (perimeter == null)
                {
                    continue;
                }

                var tiers = GetNumberOfTiers(input);
                var tierHeight = tiers > 0 ? input.BuildingHeight / (tiers + 1) : input.BuildingHeight;
                bool isValidEnvelope = Envelope.TryCreateFromZeroElevationProfile(site, perimeter, 0, tierHeight, Vector3.ZAxis, 0.0, out var zeroEnvelope);

                if (!isValidEnvelope || zeroEnvelope.Perimeter.Area() < input.MinimumTierArea)
                {
                    continue;
                }

                envelopes.Add(zeroEnvelope);
            }

            return envelopes;
        }

        private static Envelope CreateFoundationEnvelope(Envelope zeroElevationEnvelope, EnvelopeBySiteInputs input)
        {
            // An envelope cannot be invalid here because it has similar polygon to zeroElevationEnvelope.
            _ = Envelope.TryCreateFromZeroElevationProfile(zeroElevationEnvelope.Site,
                                                       zeroElevationEnvelope.Profile,
                                                       -1,
                                                       input.FoundationDepth,
                                                       Vector3.ZAxis,
                                                       0.0,
                                                       out var foundationEnvelope);
            return foundationEnvelope;
        }

        private static List<Envelope> CreateTierEnvelopes(Envelope zeroElevationEnvelope, EnvelopeBySiteInputs input)
        {
            var tiersNumber = GetNumberOfTiers(input);
            var tierEnvelopes = new List<Envelope>();

            for (int tier = 1; tier <= tiersNumber; tier++)
            {
                bool isValidEnvelope = Envelope.TryCreateFromZeroElevationProfile(zeroElevationEnvelope.Site,
                                                                                  zeroElevationEnvelope.Profile,
                                                                                  tier,
                                                                                  zeroElevationEnvelope.Height,
                                                                                  Vector3.ZAxis,
                                                                                  input.SetbackDepth,
                                                                                  out var tierEnvelope);

                if (!isValidEnvelope || tierEnvelope.Perimeter.Area() < input.MinimumTierArea)
                {
                    continue;
                }

                if (tierEnvelope.Perimeter == null || tierEnvelope.Perimeter.Area() < input.MinimumTierArea)
                {
                    break;
                }

                tierEnvelopes.Add(tierEnvelope);
            }

            return tierEnvelopes;
        }

        private static int GetNumberOfTiers(EnvelopeBySiteInputs input)
        {
            var setbackInterval = input.UseSetbacks ? input.SetbackInterval : 0;
            return setbackInterval == 0 ? 0 : (int) Math.Floor(input.BuildingHeight / setbackInterval) - 1;
        }

        private static Polygon GetSiteZeroElevationPolygon(Site site, double siteSetback, double minimumTierArea)
        {
            var perims = site.Perimeter.Offset(-siteSetback);

            if (perims.Length == 0)
            {
                return null;
            }

            var perimeter = perims.OrderByDescending(p => p.Area()).First();

            if (perimeter.Area() < minimumTierArea)
            {
                return null;
            }

            return perimeter;
        }

        private static bool IsMatchingIdentity(Envelope envelope, EnvelopeFootprintIdentity identity)
        {
            return envelope.Site.Perimeter.Contains(identity.SiteCentroid)
                   && identity.Elevation.ApproximatelyEquals(envelope.Elevation, 1.0);
        }

        private static Envelope ModifyEnvelopeFootprint(Envelope envelope, EnvelopeFootprintOverride edit)
        {
            var newZeroPolygon = edit?.Value?.Perimeter?.Project(XY_PLANE) ?? envelope.Perimeter;
            envelope.UpdateProfile(newZeroPolygon);
            return envelope;
        }
    }
}