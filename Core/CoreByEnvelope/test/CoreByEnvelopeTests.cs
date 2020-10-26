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
    /// <summary>
    /// Writes all new Elements to JSON output.
    /// Writes all new Elements and any incoming contextual Elements to GLB output.
    /// </summary>
    public class CoreByEnvelopeTests
    {
        private const string INPUT = "../../../_input/";
        private const string OUTPUT = "../../../_output/";

        [Fact]
        public void CoreByEnvelopeTest()
        {
            var model = 
                Model.FromJson(System.IO.File.ReadAllText(INPUT + "Envelope1.json"));
            var inputs = new CoreByEnvelopeInputs(
                percentageArea: 0.15, 
                lengthToWidthRatio: 0.5, 
                minimumPerimeterOffset: 1.0, 
                serviceCorePenthouseHeight: 4.0, 
                "", "", new Dictionary<string, string>(), "", "", "");
            var outputs = 
                CoreByEnvelope.Execute(new Dictionary<string, Model>{{"Envelope", model}}, inputs);
            System.IO.File.WriteAllText(OUTPUT + "CoreByEnvelope1.json", outputs.Model.ToJson());
            outputs.Model.AddElements(model.Elements.Values);
            outputs.Model.ToGlTF(OUTPUT + "CoreByEnvelope1.glb");

            model = 
                Model.FromJson(System.IO.File.ReadAllText(INPUT + "Envelope2.json"));
            inputs = 
                new CoreByEnvelopeInputs(
                    percentageArea: 0.15,
                    lengthToWidthRatio: 0.5,
                    minimumPerimeterOffset: 1.0,
                    serviceCorePenthouseHeight: 4.0,
                    "", "", new Dictionary<string, string>(), "", "", "");
            outputs = 
                CoreByEnvelope.Execute(new Dictionary<string, Model> { { "Envelope", model } }, inputs);
            System.IO.File.WriteAllText(OUTPUT + "CoreByEnvelope2.json", outputs.Model.ToJson());
            outputs.Model.AddElements(model.Elements.Values);
            outputs.Model.ToGlTF(OUTPUT + "CoreByEnvelope2.glb");

            model = 
                Model.FromJson(System.IO.File.ReadAllText(INPUT + "Envelope3.json"));
            inputs = 
                new CoreByEnvelopeInputs(
                    percentageArea: 0.15, 
                    lengthToWidthRatio: 0.5, 
                    minimumPerimeterOffset: 1.0, 
                    serviceCorePenthouseHeight: 4.0, 
                    "", "", new Dictionary<string, string>(), "", "", "");
            outputs = 
                CoreByEnvelope.Execute(new Dictionary<string, Model> { { "Envelope", model } }, inputs);
            System.IO.File.WriteAllText(OUTPUT + "CoreByEnvelope3.json",
                outputs.Model.ToJson());
            outputs.Model.AddElements(model.Elements.Values);
            outputs.Model.ToGlTF(OUTPUT + "CoreByEnvelope3.glb");
        }
    }
}
