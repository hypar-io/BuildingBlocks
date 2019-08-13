using Xunit;
using System.Linq;
using Newtonsoft.Json;
using Elements;
using Hypar.Functions.Execution.Local;
using Elements.Serialization.glTF;
using Elements.Serialization.JSON;


namespace Structure.tests
{
    public class StructureTests
    {
        [Fact]
        public void StructureTest()
        {
            var model = Model.FromJson(System.IO.File.ReadAllText("../../../../core.json"));
            var inputs = new StructureInputs(4.0, 5.0, 0.5, "", "", "", "", "");
            var outputs = Structure.Execute(model, inputs);
            System.IO.File.WriteAllText("../../../../structureCore.json", model.ToJson());
            model.ToGlTF("../../../../structureCore.glb");

            model = Model.FromJson(System.IO.File.ReadAllText("../../../../mass.json"));
            inputs = new StructureInputs(4.0, 5.0, 0.5, "", "", "", "", "");
            outputs = Structure.Execute(model, inputs);
            System.IO.File.WriteAllText("../../../../structureMass.json", model.ToJson());
            model.ToGlTF("../../../../structureMass.glb");

            model = Model.FromJson(System.IO.File.ReadAllText("../../../../story.json"));
            inputs = new StructureInputs(4.0, 5.0, 0.5, "", "", "", "", "");
            outputs = Structure.Execute(model, inputs);
            System.IO.File.WriteAllText("../../../../structureStory.json", model.ToJson());
            model.ToGlTF("../../../../structureStory.glb");
        }
    }
}
