using System;
using Elements.Geometry;
using Newtonsoft.Json;

namespace Elements
{
    public class ConceptualMass : Element
    {
        public Guid Level { get; set; }
        [JsonProperty("Top Level", Required = Required.Always)]
        public Guid TopLevel { get; set; }
        public Transform Transform { get; set; }
        public double Height { get; set; }
        public Profile Boundary { get; set; }
    }
}