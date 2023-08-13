using Newtonsoft.Json;
using System.Collections.Generic;
using Elements.Geometry;
using System;

namespace Elements
{
    public partial class LevelVolume
    {
        // This is just a convenience attachment for the proxy, so that we can grab it
        // in the code anywhere we have access to the level volume. It won't be serialized
        // with the level — but it will be added separately the model.
        [JsonIgnore]
        public ElementProxy<LevelVolume> Proxy { get; set; }
        

        [JsonProperty("Primary Use Category")]
        public string PrimaryUseCategory { get; set; }

        [JsonProperty("Add Id")]
        public string AddId { get; set; }
    }
}