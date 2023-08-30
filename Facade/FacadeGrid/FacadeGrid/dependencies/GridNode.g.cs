//----------------------
// <auto-generated>
//     Generated using the NJsonSchema v10.1.21.0 (Newtonsoft.Json v12.0.0.0) (http://NJsonSchema.org)
// </auto-generated>
//----------------------
using Elements;
using Elements.GeoJSON;
using Elements.Geometry;
using Elements.Geometry.Solids;
using Elements.Spatial;
using Elements.Validators;
using Elements.Serialization.JSON;
using System;
using System.Collections.Generic;
using System.Linq;
using Line = Elements.Geometry.Line;
using Polygon = Elements.Geometry.Polygon;

namespace Elements
{
    #pragma warning disable // Disable all warnings

    /// <summary>Represents a point at the intersection of two gridlines</summary>
    [Newtonsoft.Json.JsonConverter(typeof(Elements.Serialization.JSON.JsonInheritanceConverter), "discriminator")]
    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "10.1.21.0 (Newtonsoft.Json v12.0.0.0)")]
    public partial class GridNode : Element
    {
        [Newtonsoft.Json.JsonConstructor]
        public GridNode(Transform @location, string @uGridline, string @vGridline, System.Guid @id = default, string @name = null)
            : base(id, name)
        {
            this.Location = @location;
            this.UGridline = @uGridline;
            this.VGridline = @vGridline;
            }
        
        // Empty constructor
        public GridNode()
            : base()
        {
        }
    
        /// <summary>The location of this grid node.</summary>
        [Newtonsoft.Json.JsonProperty("Location", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public Transform Location { get; set; }
    
        [Newtonsoft.Json.JsonProperty("U Gridline", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public string UGridline { get; set; }
    
        [Newtonsoft.Json.JsonProperty("V Gridline", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public string VGridline { get; set; }
    
    
    }
}