using Xunit;
using System.Linq;
using Newtonsoft.Json;
using Elements;
using Hypar.Functions.Execution.Local;
using Elements.Serialization.glTF;
using Elements.Serialization.JSON;
using System.Collections.Generic;


namespace FloorsByEnvelope.tests
{
    public class FloorsByEnvelopeTests
    {
        [Fact]
        public void CoreTest()
        {
            var model = Model.FromJson(System.IO.File.ReadAllText("../../../../../../TestOutput/EnvelopeBySketch.json"));
            var inputs = new FloorsByEnvelopeInputs(4.0, 4.0, 1.5, 0.5, "", "", new Dictionary<string, string>(), "", "", "");
            var outputs = FloorsByEnvelope.Execute(new Dictionary<string, Model>{{"Envelope", model}}, inputs);
            System.IO.File.WriteAllText("../../../../../../TestOutput/FloorsByEnvelope.json", outputs.model.ToJson());
            outputs.model.ToGlTF("../../../../../../TestOutput/FloorsByEnvelope.glb");
        }
    }
}
