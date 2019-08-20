using Xunit;
using System.Linq;
using Newtonsoft.Json;
using Elements;
using Hypar.Functions.Execution.Local;
using Elements.Serialization.glTF;
using Elements.Serialization.JSON;
using System.Collections.Generic;


namespace Mass.tests
{
    public class MassTests
    {
        [Fact]
        public void MassTest()
        {
            var model = Model.FromJson(System.IO.File.ReadAllText("../../../../Site.json"));
            var inputs = new MassInputs(200.0, 20.0, 50.0, 5.0, "", "", new Dictionary<string, string>(), "", "", "");
            var outputs = Mass.Execute(new Dictionary<string, Model>{{"site", model}}, inputs);
            System.IO.File.WriteAllText("../../../../Mass.json", outputs.model.ToJson());
            var store = new FileModelStore<Hypar.Functions.Execution.ArgsBase>("../../../../", true);
            outputs.model.ToGlTF("../../../../Mass.glb");
        }
    }
}
