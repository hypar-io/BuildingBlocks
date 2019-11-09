// This code was generated by Hypar.
// Edits to this code will be overwritten the next time you run 'hypar init'.
// DO NOT EDIT THIS FILE.

using Elements;
using Elements.GeoJSON;
using Elements.Geometry;
using Hypar.Functions;
using Hypar.Functions.Execution;
using Hypar.Functions.Execution.AWS;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;

namespace Facade
{
    public class FacadeInputs: S3Args
    {
		/// <summary>
		/// The panel width
		/// </summary>
		[JsonProperty("Panel Width")]
		public double PanelWidth {get;}

		/// <summary>
		/// Width of each mullion.
		/// </summary>
		[JsonProperty("Mullion Width")]
		public double MullionWidth {get;}

		/// <summary>
		/// The inset of the glass panel from the outer frame.
		/// </summary>
		[JsonProperty("Glass Inset")]
		public double GlassInset {get;}


        
        /// <summary>
        /// Construct a FacadeInputs with default inputs.
        /// This should be used for testing only.
        /// </summary>
        public FacadeInputs() : base()
        {
			this.PanelWidth = 3;
			this.MullionWidth = 0.5;
			this.GlassInset = 1;

        }


        /// <summary>
        /// Construct a FacadeInputs specifying all inputs.
        /// </summary>
        /// <returns></returns>
        [JsonConstructor]
        public FacadeInputs(double panelwidth, double mullionwidth, double glassinset, string bucketName, string uploadsBucket, Dictionary<string, string> modelInputKeys, string gltfKey, string elementsKey, string ifcKey): base(bucketName, uploadsBucket, modelInputKeys, gltfKey, elementsKey, ifcKey)
        {
			this.PanelWidth = panelwidth;
			this.MullionWidth = mullionwidth;
			this.GlassInset = glassinset;

		}

		public override string ToString()
		{
			var json = JsonConvert.SerializeObject(this);
			return json;
		}
	}
}