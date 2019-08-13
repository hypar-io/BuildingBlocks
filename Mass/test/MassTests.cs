using Xunit;
using System.Linq;
using Newtonsoft.Json;
using Elements;
using Hypar.Functions.Execution.Local;
using Elements.Serialization.glTF;
using Elements.Serialization.JSON;


namespace Mass.tests
{
    public class MassTests
    {
        [Fact]
        public void MassTest()
        {
            var model = Model.FromJson(System.IO.File.ReadAllText("../../../../Site.json"));
            var inputs = new MassInputs(200.0, 20.0, 50.0, 5.0, "", "", "", "", "");
            var outputs = Mass.Execute(model, inputs);
            System.IO.File.WriteAllText("../../../../Mass.json", model.ToJson());
            var store = new FileModelStore<Hypar.Functions.Execution.ArgsBase>("../../../../", true);
            model.ToGlTF("../../../../Mass.glb");
        }
    }
}
