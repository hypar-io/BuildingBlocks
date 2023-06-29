using Elements.Geometry;
using Elements.Geometry.Solids;
using GeometryEx;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Elements
{
    public partial class Envelope
    {
        private static readonly Material FOUNDATION_MATERIAL = new ("foundation", Palette.Gray, 0.0f, 0.0f);
        private static readonly Material ENVELOPE_MATERIAL = new ("envelope", Palette.Aqua, 0.0f, 0.0f);

        // the perimeter of the profile, transformed up to the envelope's profile, for override purposes.
        public Polygon Perimeter { get; set; }
        [JsonProperty("Site Centroid")]
        public Vector3 SiteCentroid { get; set; }
        [JsonIgnore]
        public Site Site { get; private set; }

        public Envelope(Profile @profile,
                        Site @site,
                        Polygon @perimeter,
                        double @elevation,
                        double @height,
                        Vector3 @direction,
                        Transform @transform,
                        Material @material,
                        Guid @id)
            : this(@profile, @elevation, @height, @direction, 0.0, transform: @transform, material: @material, id: @id)
        {
            Site = @site;
            SiteCentroid = @site.Perimeter.Centroid();
            Perimeter = @perimeter;
        }

        public override void UpdateRepresentations()
        {
            var extrude = new Extrude(Profile.Perimeter, Height, Direction, false);
            Representation = new Representation(extrude);
        }

        public void UpdateProfile(Profile profile)
        {
            Profile = profile;
            Perimeter = profile.Perimeter.TransformedPolygon(Transform);
        }

        private static Polygon GetPolygonFromTier(Profile zeroElevationProfile, double setbackDepth, int tier)
        {
            if (tier == 0)
            {
                return zeroElevationProfile.Perimeter;
            }

            var offsetResult = zeroElevationProfile.Perimeter.Offset(-setbackDepth * Math.Abs(tier));

            if (offsetResult.Length == 0)
            {
                return null;
            }

            return offsetResult.OrderByDescending(p => p.Area()).First();
        }

        public static bool TryCreateFromZeroElevationProfile(Site site,
                                                             Profile zeroElevationProfile,
                                                             int tier,
                                                             double height,
                                                             Vector3 direction,
                                                             double setbackDepth,
                                                             out Envelope envelope)
        {
            var profile = GetPolygonFromTier(zeroElevationProfile, setbackDepth, tier);

            if (profile == null)
            {
                envelope = null;
                return false;
            }

            var id = Guid.NewGuid();
            var material = tier < 0 ? FOUNDATION_MATERIAL : ENVELOPE_MATERIAL;
            var elevation = height * tier;
            var transform = new Transform(0.0, 0.0, elevation);
            var perimeter = profile.TransformedPolygon(transform);
            envelope = new Envelope(profile, site, perimeter, elevation, height, direction, transform, material, id);

            return true;
        }
    }
}