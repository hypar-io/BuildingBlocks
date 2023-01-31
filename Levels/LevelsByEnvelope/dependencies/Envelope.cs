using System.Collections.Generic;
using Newtonsoft.Json;

namespace Elements
{
    public partial class Envelope
    {
        [JsonProperty("Floor To Floor Heights")]
        public List<double> FloorToFloorHeights { get; set; }
    }
}