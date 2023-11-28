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

namespace SubdivideSlab
{
    #pragma warning disable // Disable all warnings

    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "10.1.21.0 (Newtonsoft.Json v13.0.0.0)")]
    
    public  class SubdivideSlabInputs : S3Args
    
    {
        [Newtonsoft.Json.JsonConstructor]
        
        public SubdivideSlabInputs(double @length, double @width, bool @subdivideAtVoidCorners, bool @alignToLongestEdge, string bucketName, string uploadsBucket, Dictionary<string, string> modelInputKeys, string gltfKey, string elementsKey, string ifcKey):
        base(bucketName, uploadsBucket, modelInputKeys, gltfKey, elementsKey, ifcKey)
        {
            var validator = Validator.Instance.GetFirstValidatorForType<SubdivideSlabInputs>();
            if(validator != null)
            {
                validator.PreConstruct(new object[]{ @length, @width, @subdivideAtVoidCorners, @alignToLongestEdge});
            }
        
            this.Length = @length;
            this.Width = @width;
            this.SubdivideAtVoidCorners = @subdivideAtVoidCorners;
            this.AlignToLongestEdge = @alignToLongestEdge;
        
            if(validator != null)
            {
                validator.PostConstruct(this);
            }
        }
    
        /// <summary>The max length of each subdivision.</summary>
        [Newtonsoft.Json.JsonProperty("Length", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        [System.ComponentModel.DataAnnotations.Range(1.0D, 50.0D)]
        public double Length { get; set; } = 26D;
    
        /// <summary>The max width of each subdivision.</summary>
        [Newtonsoft.Json.JsonProperty("Width", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        [System.ComponentModel.DataAnnotations.Range(1.0D, 50.0D)]
        public double Width { get; set; } = 26D;
    
        /// <summary>If true, splits will be inserted at the corners of voids.</summary>
        [Newtonsoft.Json.JsonProperty("Subdivide at void corners", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public bool SubdivideAtVoidCorners { get; set; }
    
        /// <summary>If true, grid orientation will run parallel to the longest edge of the floor boundary.</summary>
        [Newtonsoft.Json.JsonProperty("Align to longest edge", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public bool AlignToLongestEdge { get; set; }
    
    }
}