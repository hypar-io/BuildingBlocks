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

namespace FacadeGridByLevels
{
    #pragma warning disable // Disable all warnings

    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "10.1.21.0 (Newtonsoft.Json v13.0.0.0)")]
    
    public  class FacadeGridByLevelsInputs : S3Args
    
    {
        [Newtonsoft.Json.JsonConstructor]
        
        public FacadeGridByLevelsInputs(double @offsetFromFacade, FacadeGridByLevelsInputsRemainderPosition @remainderPosition, double @targetFacadePanelWidth, FacadeGridByLevelsInputsMode @mode, FacadeGridByLevelsInputsDisplayMode @displayMode, FixedWidthSettings @fixedWidthSettings, PatternSettings @patternSettings, string bucketName, string uploadsBucket, Dictionary<string, string> modelInputKeys, string gltfKey, string elementsKey, string ifcKey):
        base(bucketName, uploadsBucket, modelInputKeys, gltfKey, elementsKey, ifcKey)
        {
            var validator = Validator.Instance.GetFirstValidatorForType<FacadeGridByLevelsInputs>();
            if(validator != null)
            {
                validator.PreConstruct(new object[]{ @offsetFromFacade, @remainderPosition, @targetFacadePanelWidth, @mode, @displayMode, @fixedWidthSettings, @patternSettings});
            }
        
            this.OffsetFromFacade = @offsetFromFacade;
            this.RemainderPosition = @remainderPosition;
            this.TargetFacadePanelWidth = @targetFacadePanelWidth;
            this.Mode = @mode;
            this.DisplayMode = @displayMode;
            this.FixedWidthSettings = @fixedWidthSettings;
            this.PatternSettings = @patternSettings;
        
            if(validator != null)
            {
                validator.PostConstruct(this);
            }
        }
    
        /// <summary>Optionally, offset the facade grid at a distance from the base envelope.</summary>
        [Newtonsoft.Json.JsonProperty("Offset From Facade", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public double OffsetFromFacade { get; set; } = 0D;
    
        /// <summary>Where do you want off panels to be positioned?</summary>
        [Newtonsoft.Json.JsonProperty("Remainder Position", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        [Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        public FacadeGridByLevelsInputsRemainderPosition RemainderPosition { get; set; } = FacadeGridByLevelsInputsRemainderPosition.At_Both_Ends;
    
        /// <summary>The Target width of the facade panels.</summary>
        [Newtonsoft.Json.JsonProperty("Target Facade Panel Width", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        [System.ComponentModel.DataAnnotations.Range(1D, 10D)]
        public double TargetFacadePanelWidth { get; set; } = 3D;
    
        /// <summary>What general strategy should be use for creating panels?</summary>
        [Newtonsoft.Json.JsonProperty("Mode", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        [Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        public FacadeGridByLevelsInputsMode Mode { get; set; } = FacadeGridByLevelsInputsMode.Approximate_Width;
    
        [Newtonsoft.Json.JsonProperty("Display Mode", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        [Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        public FacadeGridByLevelsInputsDisplayMode DisplayMode { get; set; } = FacadeGridByLevelsInputsDisplayMode.Color_By_Type;
    
        [Newtonsoft.Json.JsonProperty("Fixed Width Settings", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public FixedWidthSettings FixedWidthSettings { get; set; }
    
        [Newtonsoft.Json.JsonProperty("Pattern Settings", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public PatternSettings PatternSettings { get; set; }
    
    }
    
    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "10.1.21.0 (Newtonsoft.Json v13.0.0.0)")]
    public enum FacadeGridByLevelsInputsRemainderPosition
    {
        [System.Runtime.Serialization.EnumMember(Value = @"At Start")]
        At_Start = 0,
    
        [System.Runtime.Serialization.EnumMember(Value = @"At Both Ends")]
        At_Both_Ends = 1,
    
        [System.Runtime.Serialization.EnumMember(Value = @"At Middle")]
        At_Middle = 2,
    
        [System.Runtime.Serialization.EnumMember(Value = @"At End")]
        At_End = 3,
    
    }
    
    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "10.1.21.0 (Newtonsoft.Json v13.0.0.0)")]
    public enum FacadeGridByLevelsInputsMode
    {
        [System.Runtime.Serialization.EnumMember(Value = @"Approximate Width")]
        Approximate_Width = 0,
    
        [System.Runtime.Serialization.EnumMember(Value = @"Fixed Width")]
        Fixed_Width = 1,
    
        [System.Runtime.Serialization.EnumMember(Value = @"Pattern")]
        Pattern = 2,
    
    }
    
    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "10.1.21.0 (Newtonsoft.Json v13.0.0.0)")]
    public enum FacadeGridByLevelsInputsDisplayMode
    {
        [System.Runtime.Serialization.EnumMember(Value = @"Color By Type")]
        Color_By_Type = 0,
    
        [System.Runtime.Serialization.EnumMember(Value = @"Solid Color")]
        Solid_Color = 1,
    
        [System.Runtime.Serialization.EnumMember(Value = @"Edges Only")]
        Edges_Only = 2,
    
    }
    
    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "10.1.21.0 (Newtonsoft.Json v13.0.0.0)")]
    
    public partial class FixedWidthSettings 
    
    {
        [Newtonsoft.Json.JsonConstructor]
        public FixedWidthSettings(double @heightShift, double @panelWidth)
        {
            var validator = Validator.Instance.GetFirstValidatorForType<FixedWidthSettings>();
            if(validator != null)
            {
                validator.PreConstruct(new object[]{ @heightShift, @panelWidth});
            }
        
            this.HeightShift = @heightShift;
            this.PanelWidth = @panelWidth;
        
            if(validator != null)
            {
                validator.PostConstruct(this);
            }
        }
    
        /// <summary>If set to a value other than 0, will shift the setting out point for each set of panels by a multiple of the elevation of the level.</summary>
        [Newtonsoft.Json.JsonProperty("Height Shift", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public double HeightShift { get; set; } = 0D;
    
        /// <summary>The width of the facade panels.</summary>
        [Newtonsoft.Json.JsonProperty("Panel Width", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        [System.ComponentModel.DataAnnotations.Range(1D, 10D)]
        public double PanelWidth { get; set; } = 3D;
    
        private System.Collections.Generic.IDictionary<string, object> _additionalProperties = new System.Collections.Generic.Dictionary<string, object>();
    
        [Newtonsoft.Json.JsonExtensionData]
        public System.Collections.Generic.IDictionary<string, object> AdditionalProperties
        {
            get { return _additionalProperties; }
            set { _additionalProperties = value; }
        }
    }
    
    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "10.1.21.0 (Newtonsoft.Json v13.0.0.0)")]
    
    public partial class PatternSettings 
    
    {
        [Newtonsoft.Json.JsonConstructor]
        public PatternSettings(IList<double> @panelWidthPattern, PatternSettingsPatternMode @patternMode)
        {
            var validator = Validator.Instance.GetFirstValidatorForType<PatternSettings>();
            if(validator != null)
            {
                validator.PreConstruct(new object[]{ @panelWidthPattern, @patternMode});
            }
        
            this.PanelWidthPattern = @panelWidthPattern;
            this.PatternMode = @patternMode;
        
            if(validator != null)
            {
                validator.PostConstruct(this);
            }
        }
    
        [Newtonsoft.Json.JsonProperty("Panel Width Pattern", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public IList<double> PanelWidthPattern { get; set; }
    
        /// <summary>How should the pattern repeat? Cycle = ABCABCABC, Flip = ABCBABCBA</summary>
        [Newtonsoft.Json.JsonProperty("Pattern Mode", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        [Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        public PatternSettingsPatternMode PatternMode { get; set; } = PatternSettingsPatternMode.Cycle;
    
        private System.Collections.Generic.IDictionary<string, object> _additionalProperties = new System.Collections.Generic.Dictionary<string, object>();
    
        [Newtonsoft.Json.JsonExtensionData]
        public System.Collections.Generic.IDictionary<string, object> AdditionalProperties
        {
            get { return _additionalProperties; }
            set { _additionalProperties = value; }
        }
    }
    
    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "10.1.21.0 (Newtonsoft.Json v13.0.0.0)")]
    public enum PatternSettingsPatternMode
    {
        [System.Runtime.Serialization.EnumMember(Value = @"Cycle")]
        Cycle = 0,
    
        [System.Runtime.Serialization.EnumMember(Value = @"Flip")]
        Flip = 1,
    
    }
}