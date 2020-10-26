using Xunit;
using Elements;
using Elements.Serialization.glTF;
using System.Collections.Generic;


namespace StructureByEnvelope.tests
{
    /// <summary>
    /// Writes all new Elements to JSON output.
    /// Writes all new Elements and any incoming contextual Elements to GLB output.
    /// </summary>
    public class StructureByEnvelopeTests
    {
        private const string INPUT = "../../../_input/";
        private const string OUTPUT = "../../../_output/";

        [Fact]
        public void StructureByEnvelopeTest()
        {
            var envModel = Model.FromJson(System.IO.File.ReadAllText(INPUT + "Envelope.json"));
            var lvlModel = Model.FromJson(System.IO.File.ReadAllText(INPUT + "Levels.json"));
            var inputs = new StructureByEnvelopeInputs(
                gridXAxisInterval: 4.0, 
                gridYAxisInterval: 5.0,
                slabEdgeOffset: 1.0,
                "", "", new Dictionary<string, string>(), "", "", "");
            var outputs = StructureByEnvelope.Execute(
                new Dictionary<string, Model>
                {
                    {"Envelope", envModel},
                    {"Levels", lvlModel}
                }, inputs);
            System.IO.File.WriteAllText(OUTPUT + "StructureByEnvelope.json", outputs.Model.ToJson());
            outputs.Model.AddElements(envModel.Elements.Values);
            outputs.Model.AddElements(lvlModel.Elements.Values);
            outputs.Model.ToGlTF(OUTPUT + "StructureByEnvelope.glb");
        }
    }
}
