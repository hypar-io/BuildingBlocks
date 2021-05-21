using Xunit;
using Elements;
using Elements.Serialization.glTF;
using System.Collections.Generic;


namespace FloorsByLevels.tests
{
    /// <summary>
    /// Writes all new Elements to JSON output.
    /// Writes all new Elements and any incoming contextual Elements to GLB output.
    /// </summary>
    public class FloorsByLevelsTests
    {
        private const string INPUT = "../../../_input/";
        private const string OUTPUT = "../../../_output/";

        [Fact]
        public void FloorsByLevelsTest()
        {
            var model =
                Model.FromJson(System.IO.File.ReadAllText(INPUT + "LevelsByEnvelope.json"));
            var inputs =
                new FloorsByLevelsInputs(
                    floorSetback: 0.2,
                    floorThickness: 0.1,
                    "", "", new Dictionary<string, string>(), "", "", "");
            var outputs =
                FloorsByLevels.Execute(new Dictionary<string, Model> { { "Levels", model } }, inputs);
            System.IO.File.WriteAllText(OUTPUT + "FloorsByLevels.json", outputs.Model.ToJson());
            outputs.Model.ToGlTF(OUTPUT + "FloorsByLevels.glb");
        }

        [Fact]
        public void GHFloorsByLevelsTest()
        {
            var model = Model.FromJson(System.IO.File.ReadAllText(INPUT + "Building-01.json"));
            var inputs = new FloorsByLevelsInputs(0.2, 0.5, "", "", new Dictionary<string, string>(), "", "", "");
            var outputs = FloorsByLevels.Execute(new Dictionary<string, Model> { { "Levels", model } }, inputs);
            System.IO.File.WriteAllText(OUTPUT + "GHFloorsByLevels.json", outputs.Model.ToJson());
            outputs.Model.AddElements(model.Elements.Values);
            outputs.Model.ToGlTF(OUTPUT + "GHFloorsByLevels.glb");
        }
    }
}
