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
            var inputs = new FloorsByLevelsInputs(0.5, "", "", new Dictionary<string, string>(), "", "", "");
            var outputs = FloorsByLevels.Execute(new Dictionary<string, Model>{{"Levels", model}}, inputs);
            System.IO.File.WriteAllText("../../../../../../TestOutput/FloorsByLevels.json", outputs.model.ToJson());
            outputs.model.ToGlTF("../../../../../../TestOutput/FloorsByLevels.glb");
        }
    }
}
