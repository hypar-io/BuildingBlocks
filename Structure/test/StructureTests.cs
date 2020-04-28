using Xunit;
using Elements;
using Elements.Serialization.glTF;
using System.Collections.Generic;


namespace Structure.tests
{
    public class StructureTests
    {
        [Fact]
        public void StructureTest()
        {
            var model = Model.FromJson(System.IO.File.ReadAllText("../../../../core.json"));
            var inputs = new StructureInputs(4.0, 5.0, "", "", new Dictionary<string, string>(), "", "", "");
            var outputs = Structure.Execute(new Dictionary<string, Model> { { "envelope", model } }, inputs);
            System.IO.File.WriteAllText("../../../../structureCore.json", outputs.Model.ToJson());
            outputs.Model.ToGlTF("../../../../structureCore.glb");

            model = Model.FromJson(System.IO.File.ReadAllText("../../../../mass.json"));
            inputs = new StructureInputs(4.0, 5.0, "", "", new Dictionary<string, string>(), "", "", "");
            outputs = Structure.Execute(new Dictionary<string, Model> { { "envelope", model } }, inputs);
            System.IO.File.WriteAllText("../../../../structureMass.json", outputs.Model.ToJson());
            outputs.Model.ToGlTF("../../../../structureMass.glb");

            model = Model.FromJson(System.IO.File.ReadAllText("../../../../story.json"));
            inputs = new StructureInputs(4.0, 5.0, "", "", new Dictionary<string, string>(), "", "", "");
            outputs = Structure.Execute(new Dictionary<string, Model> { { "envelope", model } }, inputs);
            System.IO.File.WriteAllText("../../../../structureStory.json", outputs.Model.ToJson());
            outputs.Model.ToGlTF("../../../../structureStory.glb");
        }
    }
}
