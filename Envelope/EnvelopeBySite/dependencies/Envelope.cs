using Elements.Geometry;
using Newtonsoft.Json;

namespace Elements
{
    public partial class Envelope
    {
        // the perimeter of the profile, transformed up to the envelope's profile, for override purposes.
        public Polygon Perimeter { get; set; }
        [JsonProperty("Site Centroid")]
        public Vector3 SiteCentroid { get; set; }
    }
}