using Xunit;
using Elements;
using Elements.Serialization.glTF;
using System.Collections.Generic;


namespace Structure.tests
{
    /// <summary>
    /// Writes all new Elements to JSON output.
    /// Writes all new Elements and any incoming contextual Elements to GLB output.
    /// </summary>
    public class StructureTests
    {
        private const string INPUT = "../../../_input/";
        private const string OUTPUT = "../../../_output/";

        [Fact]
        public void StructureTest()
        {
            var envModel = Model.FromJson(System.IO.File.ReadAllText(INPUT + "Envelope.json"));
            var lvlModel = Model.FromJson(System.IO.File.ReadAllText(INPUT + "Levels.json"));
            var inputs = new StructureInputs(
                5.0,
                6.0,
                1.5,
                false,
                StructureInputsTypeOfConstruction.Steel,
                StructureInputsColumnType.W10x100,
                StructureInputsGirderType.W10x100,
                StructureInputsBeamType.W10x100,
                1.5,
                false,
                0.1254,
                false,
                maximumNeighborSpan: 2,
                modelInputKeys: new Dictionary<string, string>(), gltfKey: "", elementsKey: "", ifcKey: "");
            var outputs = Structure.Execute(
                new Dictionary<string, Model>
                {
                    {"Envelope", envModel},
                    {"Levels", lvlModel}
                }, inputs);
            System.IO.File.WriteAllText(OUTPUT + "Structure.json", outputs.Model.ToJson());
            outputs.Model.AddElements(envModel.Elements.Values);
            outputs.Model.AddElements(lvlModel.Elements.Values);
            outputs.Model.ToGlTF(OUTPUT + "Structure.glb");
        }
    }
}
