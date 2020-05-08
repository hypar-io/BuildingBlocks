using Xunit;
using Elements;
using Elements.Serialization.glTF;
using System.Collections.Generic;


namespace StructureByEnvelope.tests
{
    public class StructureByEnvelopeTests
    {
        [Fact]
        public void StructureByEnvelopeTest()
        {
            var model = Model.FromJson(System.IO.File.ReadAllText("../../../../core.json"));
            var inputs = new StructureByEnvelopeInputs(4.0, 5.0, "", "", new Dictionary<string, string>(), "", "", "");
            var outputs = StructureByEnvelope.Execute(new Dictionary<string, Model>{{"envelope", model}}, inputs);
            System.IO.File.WriteAllText("../../../../structureCore.json", outputs.Model.ToJson());
            outputs.Model.ToGlTF("../../../../structureCore.glb");

            model = Model.FromJson(System.IO.File.ReadAllText("../../../../mass.json"));
            inputs = new StructureByEnvelopeInputs(4.0, 5.0, "", "", new Dictionary<string, string>(), "", "", "");
            outputs = StructureByEnvelope.Execute(new Dictionary<string, Model>{{"envelope", model}}, inputs);
            System.IO.File.WriteAllText("../../../../structureMass.json", outputs.Model.ToJson());
            outputs.Model.ToGlTF("../../../../structureMass.glb");

            model = Model.FromJson(System.IO.File.ReadAllText("../../../../story.json"));
            inputs = new StructureByEnvelopeInputs(4.0, 5.0, "", "", new Dictionary<string, string>(), "", "", "");
            outputs = StructureByEnvelope.Execute(new Dictionary<string, Model>{{"envelope", model}}, inputs);
            System.IO.File.WriteAllText("../../../../structureStory.json", outputs.Model.ToJson());
            outputs.Model.ToGlTF("../../../../structureStory.glb");
        }
    }
}
