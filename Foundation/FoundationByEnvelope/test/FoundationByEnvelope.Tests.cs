using Xunit;
using System.Linq;
using Newtonsoft.Json;
using Elements;
using Hypar.Functions.Execution.Local;
using Elements.Serialization.glTF;
using Elements.Serialization.JSON;
using System.Collections.Generic;
using System;

namespace FoundationByEnvelope.Tests
{
    /// <summary>
    /// Writes all new Elements to JSON output.
    /// Writes all new Elements and any incoming contextual Elements to GLB output.
    /// </summary>
    public class FoundationByEnvelopeTests
    {
        private const string INPUT = "../../../_input/";
        private const string OUTPUT = "../../../_output/";

        [Fact]
        public void FoundationByEnvelopeTest()
        {
            var inputs 
                = new FoundationByEnvelopeInputs(
                    minDepth: 5.0, 
                    "", "", new Dictionary<string, string>(), "", "", "");
            var model =
                Model.FromJson(System.IO.File.ReadAllText(INPUT + "Envelope.json"));
            var outputs =
                FoundationByEnvelope.Execute(new Dictionary<string, Model> { { "Envelope", model } }, inputs);
            System.IO.File.WriteAllText(OUTPUT + "FoundationByEnvelope.json", outputs.Model.ToJson());
            outputs.Model.AddElements(model.Elements.Values);
            outputs.Model.ToGlTF(OUTPUT + "FoundationByEnvelope.glb");
        }
    }
}
