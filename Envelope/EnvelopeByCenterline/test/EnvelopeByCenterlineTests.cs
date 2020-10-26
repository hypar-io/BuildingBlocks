using Xunit;
using System.Linq;
using Newtonsoft.Json;
using Elements;
using Hypar.Functions.Execution.Local;
using Elements.Serialization.glTF;
using Elements.Serialization.JSON;
using System.Collections.Generic;
using Elements.Geometry;


namespace EnvelopeByCenterline.Tests
{
    /// <summary>
    /// Writes all new Elements to JSON output.
    /// Writes all new Elements and any incoming contextual Elements to GLB output.
    /// </summary>
    public class EnvelopeByCenterlineTests
    {
        private const string OUTPUT = "../../../_output/";

        [Fact]
        public void EnvelopeByCenterlineTest()
        {
            var polyline = 
                new Polygon
                (
                    new []
                    {
                        Vector3.Origin,
                        new Vector3(25.0, 0.0),
                        new Vector3(55.0, 20.0),
                        new Vector3(85.0, 60.0)                     
                    });
            
            var inputs =
                new EnvelopeByCenterlineInputs(
                    centerline: polyline, 
                    buildingHeight: 55.0, 
                    barWidth: 25.0, 
                    foundationDepth: 5.0, 
                    "", "", new Dictionary<string, string>(), "", "", "");
            var outputs = 
                EnvelopeByCenterline.Execute(new Dictionary<string, Model> { { "Envelope", new Model() } }, inputs);
            System.IO.File.WriteAllText(OUTPUT + "EnvelopeByCenterline.json", outputs.Model.ToJson());
            outputs.Model.ToGlTF(OUTPUT + "EnvelopeByCenterline.glb");
        }
    }
}
