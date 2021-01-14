//----------------------
// <auto-generated>
//     Generated using the NJsonSchema v10.1.21.0 (Newtonsoft.Json v12.0.0.0) (http://NJsonSchema.org)
// </auto-generated>
//----------------------
using Elements;
using Elements.GeoJSON;
using Elements.Geometry;
using Elements.Geometry.Solids;
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

    /// <summary>Represents one part of a building enclosure.</summary>
    [Newtonsoft.Json.JsonConverter(typeof(Elements.Serialization.JSON.JsonInheritanceConverter), "discriminator")]
    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "10.1.21.0 (Newtonsoft.Json v12.0.0.0)")]
    public partial class Envelope : GeometricElement
    {
        [Newtonsoft.Json.JsonConstructor]
        public Envelope(Profile @profile, double @elevation, double @height, Vector3 @direction, double @rotation, Transform @transform, Material @material, Representation @representation, bool @isElementDefinition, System.Guid @id, string @name)
            : base(transform, material, representation, isElementDefinition, id, name)
        {
            var validator = Validator.Instance.GetFirstValidatorForType<Envelope>();
            if(validator != null)
            {
                validator.PreConstruct(new object[]{ @profile, @elevation, @height, @direction, @rotation, @transform, @material, @representation, @isElementDefinition, @id, @name});
            }
        
            this.Profile = @profile;
            this.Elevation = @elevation;
            this.Height = @height;
            this.Direction = @direction;
            this.Rotation = @rotation;
            
            if(validator != null)
            {
                validator.PostConstruct(this);
            }
        }
    
        /// <summary>The id of the profile to extrude.</summary>
        [Newtonsoft.Json.JsonProperty("Profile", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public Profile Profile { get; set; }
    
        /// <summary>The elevation of the envelope.</summary>
        [Newtonsoft.Json.JsonProperty("Elevation", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        [System.ComponentModel.DataAnnotations.Range(0D, double.MaxValue)]
        public double Elevation { get; set; }
    
        /// <summary>The height of the envelope.</summary>
        [Newtonsoft.Json.JsonProperty("Height", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        [System.ComponentModel.DataAnnotations.Range(0D, double.MaxValue)]
        public double Height { get; set; }
    
        /// <summary>The direction in which to extrude.</summary>
        [Newtonsoft.Json.JsonProperty("Direction", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public Vector3 Direction { get; set; }
    
        /// <summary>The rotation in degrees of the envelope.</summary>
        [Newtonsoft.Json.JsonProperty("Rotation", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public double Rotation { get; set; }
    
    
    }
}