//----------------------
// <auto-generated>
//     Generated using the NJsonSchema v10.1.21.0 (Newtonsoft.Json v13.0.0.0) (http://NJsonSchema.org)
// </auto-generated>
//----------------------
using Elements;
using Elements.GeoJSON;
using Elements.Geometry;
using Elements.Geometry.Solids;
using Elements.Spatial;
using Elements.Validators;
using Elements.Serialization.JSON;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using Line = Elements.Geometry.Line;
using Polygon = Elements.Geometry.Polygon;

namespace Elements
{
    #pragma warning disable // Disable all warnings

    /// <summary>A perimeter excluding other elements.</summary>
    [JsonConverter(typeof(Elements.Serialization.JSON.JsonInheritanceConverter), "discriminator")]
    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "10.1.21.0 (Newtonsoft.Json v13.0.0.0)")]
    public partial class Exclusion : Element
    {
        [JsonConstructor]
        public Exclusion(Polygon @perimeter, double @elevation, System.Guid @id = default, string @name = null)
            : base(id, name)
        {
            this.Perimeter = @perimeter;
            this.Elevation = @elevation;
            }
        
        // Empty constructor
        public Exclusion()
            : base()
        {
        }
    
        /// <summary>The Exclusion perimeter.</summary>
        [JsonProperty("Perimeter", Required = Newtonsoft.Json.Required.AllowNull)]
        public Polygon Perimeter { get; set; }
    
        /// <summary>The elevation in meters of the Exclusion.</summary>
        [JsonProperty("Elevation", Required = Newtonsoft.Json.Required.Always)]
        public double Elevation { get; set; }
    
    
    }
}