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
    public class EnvelopeBySiteTests
    {
        [Fact]
        public void EnvelopeBySiteTest()
        {
            var model = Model.FromJson(System.IO.File.ReadAllText("../../../../../../TestOutput/SiteBySketch.json"));
            var inputs = new EnvelopeBySiteInputs(3.0, 60, 10, 1.0, 100.0, 5.0, "", "", new Dictionary<string, string>(), "", "", "");
            var outputs = EnvelopeBySite.Execute(new Dictionary<string, Model>{{"Site", model}}, inputs);
            System.IO.File.WriteAllText("../../../../../../TestOutput/EnvelopeBySite.json", outputs.Model.ToJson());
            outputs.Model.ToGlTF("../../../../../../TestOutput/EnvelopeBySite.glb");
        }
    }
}
