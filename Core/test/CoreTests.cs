using Xunit;
using System.Linq;
using Newtonsoft.Json;
using Elements;
using Hypar.Functions.Execution.Local;
using Elements.Serialization.glTF;
using Elements.Serialization.JSON;


namespace Core.tests
{
    public class CoreTests
    {
        [Fact]
        public void CoreTest()
        {
            var model = Model.FromJson(System.IO.File.ReadAllText("../../../../mass.json"));
            var inputs = new CoreInputs(5.0, 7.0, 2.5, "", "", "", "", "");
            var outputs = Core.Execute(model, inputs);
            System.IO.File.WriteAllText("../../../../core.json", model.ToJson());
            model.ToGlTF("../../../../core.glb");
        }
    }
}
