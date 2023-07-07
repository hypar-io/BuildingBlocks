using Elements;
using System;
using System.Linq;
using System.Collections.Generic;
using Elements.Geometry;
using Newtonsoft.Json;

namespace Elements
{
    public partial class LevelVolume
    {
        [JsonProperty("Add Id")]
        public string AddId { get; set; }
    }
}