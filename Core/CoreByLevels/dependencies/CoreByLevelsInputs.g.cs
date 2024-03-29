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

namespace CoreByLevels
{
    #pragma warning disable // Disable all warnings

    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "10.1.21.0 (Newtonsoft.Json v13.0.0.0)")]
    
    public  class CoreByLevelsInputs : S3Args
    
    {
        [Newtonsoft.Json.JsonConstructor]
        
        public CoreByLevelsInputs(double @setback, double @rotation, string bucketName, string uploadsBucket, Dictionary<string, string> modelInputKeys, string gltfKey, string elementsKey, string ifcKey):
        base(bucketName, uploadsBucket, modelInputKeys, gltfKey, elementsKey, ifcKey)
        {
            var validator = Validator.Instance.GetFirstValidatorForType<CoreByLevelsInputs>();
            if(validator != null)
            {
                validator.PreConstruct(new object[]{ @setback, @rotation});
            }
        
            this.Setback = @setback;
            this.Rotation = @rotation;
        
            if(validator != null)
            {
                validator.PostConstruct(this);
            }
        }
    
        /// <summary>Core perimeter setback from envelope.</summary>
        [Newtonsoft.Json.JsonProperty("Setback", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        [System.ComponentModel.DataAnnotations.Range(1.0D, 10.0D)]
        public double Setback { get; set; } = 6D;
    
        /// <summary>Core Rotation.</summary>
        [Newtonsoft.Json.JsonProperty("Rotation", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        [System.ComponentModel.DataAnnotations.Range(0.0D, 355.0D)]
        public double Rotation { get; set; } = 180D;
    
    }
}