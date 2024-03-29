// This code was generated by Hypar.
// Edits to this code will be overwritten the next time you run 'hypar init'.
// DO NOT EDIT THIS FILE.

using Elements;
using Elements.GeoJSON;
using Elements.Geometry;
using Elements.Geometry.Solids;
using Elements.Validators;
using Elements.Serialization.JSON;
using Hypar.Functions;
using Hypar.Functions.Execution;
using Hypar.Functions.Execution.AWS;
using Hypar.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using Line = Elements.Geometry.Line;
using Polygon = Elements.Geometry.Polygon;

namespace EnvelopeByCenterline
{
    #pragma warning disable // Disable all warnings

    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "10.1.21.0 (Newtonsoft.Json v13.0.0.0)")]
    
    public  class EnvelopeByCenterlineInputs : S3Args
    
    {
        [Newtonsoft.Json.JsonConstructor]
        
        public EnvelopeByCenterlineInputs(Polyline @centerline, double @buildingHeight, double @barWidth, double @foundationDepth, string bucketName, string uploadsBucket, Dictionary<string, string> modelInputKeys, string gltfKey, string elementsKey, string ifcKey):
        base(bucketName, uploadsBucket, modelInputKeys, gltfKey, elementsKey, ifcKey)
        {
            var validator = Validator.Instance.GetFirstValidatorForType<EnvelopeByCenterlineInputs>();
            if(validator != null)
            {
                validator.PreConstruct(new object[]{ @centerline, @buildingHeight, @barWidth, @foundationDepth});
            }
        
            this.Centerline = @centerline;
            this.BuildingHeight = @buildingHeight;
            this.BarWidth = @barWidth;
            this.FoundationDepth = @foundationDepth;
        
            if(validator != null)
            {
                validator.PostConstruct(this);
            }
        }
    
        /// <summary>Centerline of the building envelope.</summary>
        [Newtonsoft.Json.JsonProperty("Centerline", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public Polyline Centerline { get; set; }
    
        /// <summary>Overall height of the building from grade.</summary>
        [Newtonsoft.Json.JsonProperty("Building Height", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        [System.ComponentModel.DataAnnotations.Range(5.0D, 100.0D)]
        public double BuildingHeight { get; set; } = 52D;
    
        /// <summary>Width of the mass perpendicular to the Centerline.</summary>
        [Newtonsoft.Json.JsonProperty("Bar Width", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        [System.ComponentModel.DataAnnotations.Range(10.0D, 30.0D)]
        public double BarWidth { get; set; } = 20D;
    
        /// <summary>Depth of the building envelope below grade.</summary>
        [Newtonsoft.Json.JsonProperty("Foundation Depth", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        [System.ComponentModel.DataAnnotations.Range(5.0D, 20.0D)]
        public double FoundationDepth { get; set; } = 12D;
    
    }
}