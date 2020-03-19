using Xunit;
using System;
using System.Linq;
using Newtonsoft.Json;
using Elements;
using Elements.Geometry;
using Hypar.Functions.Execution.Local;
using Elements.Serialization.glTF;
using Elements.Serialization.JSON;
using System.Collections.Generic;


namespace CoreByEnvelope.tests
{
    public class CoreByEnvelopeTests
    {
        [Fact]
        public void CoreByEnvelopeTest()
        {
            var model = Model.FromJson(System.IO.File.ReadAllText("../../../../test/envelope1.json"));
            var inputs = new CoreByEnvelopeInputs(0.15, 0.5, 1.0, "", "", new Dictionary<string, string>(), "", "", "");
            var outputs = CoreByEnvelope.Execute(new Dictionary<string, Model>{{"Envelope", model}}, inputs);
            System.IO.File.WriteAllText("../../../../test/CoreByEnvelope1.json", outputs.model.ToJson());
            outputs.model.ToGlTF("../../../../test/CoreByEnvelope1.glb");

            model = Model.FromJson(System.IO.File.ReadAllText("../../../../test/envelope2.json"));
            inputs = new CoreByEnvelopeInputs(0.15, 0.5, 1.0, "", "", new Dictionary<string, string>(), "", "", "");
            outputs = CoreByEnvelope.Execute(new Dictionary<string, Model> { { "Envelope", model } }, inputs);
            System.IO.File.WriteAllText("../../../../test/CoreByEnvelope2.json", outputs.model.ToJson());
            outputs.model.ToGlTF("../../../../test/CoreByEnvelope2.glb");

            model = Model.FromJson(System.IO.File.ReadAllText("../../../../test/envelope3.json"));
            inputs = new CoreByEnvelopeInputs(0.15, 0.5, 1.0, "", "", new Dictionary<string, string>(), "", "", "");
            outputs = CoreByEnvelope.Execute(new Dictionary<string, Model> { { "Envelope", model } }, inputs);
            System.IO.File.WriteAllText("../../../../test/CoreByEnvelope3.json", outputs.model.ToJson());
            outputs.model.ToGlTF("../../../../test/CoreByEnvelope3.glb");
        }
    }
}
