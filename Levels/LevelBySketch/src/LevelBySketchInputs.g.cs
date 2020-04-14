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

namespace LevelBySketch
{
    public class LevelBySketchInputs: S3Args
    {
		/// <summary>
		/// Perimeter of the Level.
		/// </summary>
		[JsonProperty("Perimeter")]
		public Elements.Geometry.Polygon Perimeter {get;}


        
        /// <summary>
        /// Construct a LevelBySketchInputs with default inputs.
        /// This should be used for testing only.
        /// </summary>
        public LevelBySketchInputs() : base()
        {
			this.Perimeter = Elements.Geometry.Polygon.Rectangle(1, 1);

        }


        /// <summary>
        /// Construct a LevelBySketchInputs specifying all inputs.
        /// </summary>
        /// <returns></returns>
        [JsonConstructor]
        public LevelBySketchInputs(Elements.Geometry.Polygon perimeter, string bucketName, string uploadsBucket, Dictionary<string, string> modelInputKeys, string gltfKey, string elementsKey, string ifcKey): base(bucketName, uploadsBucket, modelInputKeys, gltfKey, elementsKey, ifcKey)
        {
			this.Perimeter = perimeter;

		}

		public override string ToString()
		{
			var json = JsonConvert.SerializeObject(this);
			return json;
		}
	}
}