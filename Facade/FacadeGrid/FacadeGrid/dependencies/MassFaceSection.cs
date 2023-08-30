using System;
using Elements.Geometry;
using Newtonsoft.Json;

namespace Elements
{

    public partial class MassFaceSection : Element
    {
        public Vector3 Normal { get; set; }
        public Vector3 Centroid { get; set; }

        [JsonProperty("Element Centroid")]
        public Vector3 ElementCentroid { get; set; }

        public FacadeGrid.Grid2dInput Grid { get; set; }

        [JsonProperty("Facade Type Name")]
        public string FacadeTypeName { get; set; }

        [JsonProperty("Facade Type")]
        public Guid FacadeType { get; set; }

        public void GenerateOverrideIdentityProperties(Vector3 elementCentroid)
        {
            var perim = Profile.Perimeter;
            this.Normal = perim.Normal();
            this.Centroid = perim.Centroid();
            this.ElementCentroid = elementCentroid;
        }

    }
}