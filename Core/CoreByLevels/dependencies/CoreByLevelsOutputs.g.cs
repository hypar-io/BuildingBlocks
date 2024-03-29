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

namespace CoreByLevels
{
    public class CoreByLevelsOutputs: SystemResults
    {
		/// <summary>
		/// Restroom quantity.
		/// </summary>
		[JsonProperty("Restrooms")]
		public double Restrooms {get; set;}

		/// <summary>
		/// Lift quantity.
		/// </summary>
		[JsonProperty("Lifts")]
		public double Lifts {get; set;}



        /// <summary>
        /// Construct a CoreByLevelsOutputs with default inputs.
        /// This should be used for testing only.
        /// </summary>
        public CoreByLevelsOutputs() : base()
        {

        }


        /// <summary>
        /// Construct a CoreByLevelsOutputs specifying all inputs.
        /// </summary>
        /// <returns></returns>
        [JsonConstructor]
        public CoreByLevelsOutputs(double restrooms, double lifts): base()
        {
			this.Restrooms = restrooms;
			this.Lifts = lifts;

		}

		public override string ToString()
		{
			var json = JsonConvert.SerializeObject(this);
			return json;
		}
	}
}