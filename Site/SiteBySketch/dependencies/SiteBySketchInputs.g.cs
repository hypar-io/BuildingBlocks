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
using System;
using System.Collections.Generic;
using System.Linq;
using Line = Elements.Geometry.Line;
using Polygon = Elements.Geometry.Polygon;

namespace SiteBySketch
{
    #pragma warning disable // Disable all warnings

    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "10.1.21.0 (Newtonsoft.Json v12.0.0.0)")]
    
    public  class SiteBySketchInputs : S3Args
    
    {
        [Newtonsoft.Json.JsonConstructor]
        
        public SiteBySketchInputs(Polygon @perimeter, string bucketName, string uploadsBucket, Dictionary<string, string> modelInputKeys, string gltfKey, string elementsKey, string ifcKey):
        base(bucketName, uploadsBucket, modelInputKeys, gltfKey, elementsKey, ifcKey)
        {
            var validator = Validator.Instance.GetFirstValidatorForType<SiteBySketchInputs>();
            if(validator != null)
            {
                validator.PreConstruct(new object[]{ @perimeter});
            }
        
            this.Perimeter = @perimeter;
        
            if(validator != null)
            {
                validator.PostConstruct(this);
            }
        }
    
        /// <summary>Perimeter of the building envelope.</summary>
        [Newtonsoft.Json.JsonProperty("Perimeter", Required = Newtonsoft.Json.Required.AllowNull)]
        public Polygon Perimeter { get; set; }
    
    
    }
}