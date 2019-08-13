using Xunit;
using System.Linq;
using Newtonsoft.Json;
using Elements;
using Hypar.Functions.Execution.Local;
using Elements.Serialization.glTF;
using Elements.Serialization.JSON;


namespace Facade.tests
{
    public class FacadeTests
    {
        [Fact]
        public void FacadeTest()
        {
            var model = Model.FromJson(System.IO.File.ReadAllText("../../../../mass.json"));
            var inputs = new FacadeInputs(4.0, 3.0, 0.2, "", "", "", "", "");
            var outputs = Facade.Execute(model, inputs);
            System.IO.File.WriteAllText("../../../../facade.json", model.ToJson());
            model.ToGlTF("../../../../facade.glb");
        }
    }
}
