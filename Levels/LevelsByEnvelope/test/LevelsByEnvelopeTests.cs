using Xunit;
using System.Linq;
using Newtonsoft.Json;
using Elements;
using Hypar.Functions.Execution.Local;
using Elements.Serialization.glTF;
using Elements.Serialization.JSON;
using System.Collections.Generic;


namespace LevelsByEnvelope.Tests
{
    public class LevelsByEnvelopeTests
    {
        [Fact]
        public void LevelsByEnvelopeTest()
        {
            var inputs = new LevelsByEnvelopeInputs(3.0, 3.0, 3.0, "", "", new Dictionary<string, string>(), "", "", "");
            var model = Model.FromJson(System.IO.File.ReadAllText("../../../../../../TestOutput/EnvelopeBySketch.json"));
            var outputs = LevelsByEnvelope.Execute(new Dictionary<string, Model>{{"Envelope", model}}, inputs);
            System.IO.File.WriteAllText("../../../../../../TestOutput/LevelsByEnvelope.json", outputs.Model.ToJson());
            outputs.Model.ToGlTF("../../../../../../TestOutput/LevelsByEnvelope.glb");
        }
    }
}
