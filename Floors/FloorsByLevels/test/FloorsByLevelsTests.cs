using Xunit;
using System.Linq;
using Newtonsoft.Json;
using Elements;
using Hypar.Functions.Execution.Local;
using Elements.Serialization.glTF;
using Elements.Serialization.JSON;
using System.Collections.Generic;


namespace FloorsByLevels.tests
{
    public class FloorsByLevelsTests
    {
        [Fact]
        public void FloorsByLevelsTest()
        {
            var model = Model.FromJson(System.IO.File.ReadAllText("../../../../../../TestOutput/LevelsByEnvelope.json"));
            var inputs = new FloorsByLevelsInputs(0.2, 0.5, "", "", new Dictionary<string, string>(), "", "", "");
            var outputs = FloorsByLevels.Execute(new Dictionary<string, Model>{{"Levels", model}}, inputs);
            System.IO.File.WriteAllText("../../../../../../TestOutput/FloorsByLevels.json", outputs.Model.ToJson());
            outputs.Model.ToGlTF("../../../../../../TestOutput/FloorsByLevels.glb");
        }

        [Fact]
        public void GHFloorsByLevelsTest()
        {
            var model = Model.FromJson(System.IO.File.ReadAllText("../../../../../../TestOutput/Building-01.json"));
            var inputs = new FloorsByLevelsInputs(0.2, 0.5, "", "", new Dictionary<string, string>(), "", "", "");
            var outputs = FloorsByLevels.Execute(new Dictionary<string, Model> { { "Levels", model } }, inputs);
            System.IO.File.WriteAllText("../../../../../../TestOutput/GHFloorsByLevels.json", outputs.Model.ToJson());
            outputs.Model.ToGlTF("../../../../../../TestOutput/GHFloorsByLevels.glb");
        }
    }
}
