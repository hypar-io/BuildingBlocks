using Xunit;
using System.Linq;
using Newtonsoft.Json;
using Elements;
using Hypar.Functions.Execution.Local;
using Elements.Serialization.glTF;
using Elements.Serialization.JSON;
using System.Collections.Generic;


namespace FacadeByEnvelope.Tests
{
    /// <summary>
    /// Writes all new Elements to JSON output.
    /// Writes all new Elements and any incoming contextual Elements to GLB output.
    /// </summary>
    public class FacadeByEnvelopeTests
    {
        private const string INPUT = "../../../_input/";
        private const string OUTPUT = "../../../_output/";

        [Fact]
        public void FacadeByEnvelopeTest()
        {
            var inputs 
                = new FacadeByEnvelopeInputs(
                    panelWidth: 3.0, 
                    glassLeftRightInset: 3.0, 
                    glassTopBottomInset: 3.0, 
                    "", "", new Dictionary<string, string>(), "", "", "");
            var envModel = Model.FromJson(System.IO.File.ReadAllText(INPUT + "Envelope.json"));
            var lvlModel = Model.FromJson(System.IO.File.ReadAllText(INPUT + "Levels.json"));
            var outputs = FacadeByEnvelope.Execute(
                new Dictionary<string, Model>
                {
                    {"Envelope", envModel},
                    {"Levels", lvlModel}
                }, 
                inputs);
            System.IO.File.WriteAllText(OUTPUT + "FacadeByEnvelope.json", outputs.Model.ToJson());
            outputs.Model.AddElements(envModel.Elements.Values);
            outputs.Model.AddElements(lvlModel.Elements.Values);
            outputs.Model.ToGlTF(OUTPUT + "FacadeByEnvelope.glb");
        }
    }
}
