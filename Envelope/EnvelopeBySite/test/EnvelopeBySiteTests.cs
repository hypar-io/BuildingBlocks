using Xunit;
using System.Linq;
using Newtonsoft.Json;
using Elements;
using Elements.Geometry;
using Hypar.Functions.Execution.Local;
using Elements.Serialization.glTF;
using Elements.Serialization.JSON;
using System.Collections.Generic;


namespace EnvelopeBySite.Tests
{
    /// <summary>
    /// Writes all new Elements to JSON output.
    /// Writes all new Elements and any incoming contextual Elements to GLB output.
    /// </summary>
    public class EnvelopeBySiteTests
    {
        private const string INPUT = "../../../_input/";
        private const string OUTPUT = "../../../_output/";

        [Fact]
        public void EnvelopeBySiteTest()
        {
            var model = Model.FromJson(System.IO.File.ReadAllText(INPUT + "SiteBySketch.json"));
            var inputs = new EnvelopeBySiteInputs(
                60,
                5,
                true,
                3.0,
                10,
                1,
                100,
                "",
                "",
                new Dictionary<string, string>(),
                "",
                "",
                ""
            );
            var outputs = EnvelopeBySite.Execute(new Dictionary<string, Model> { { "Site", model } }, inputs);
            System.IO.File.WriteAllText(OUTPUT + "EnvelopeBySite.json", outputs.Model.ToJson());
            outputs.Model.AddElements(model.Elements.Values);
            outputs.Model.ToGlTF(OUTPUT + "EnvelopeBySite.glb");
        }
    }
}
