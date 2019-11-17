using Xunit;
using System.Linq;
using Newtonsoft.Json;
using Elements;
using Hypar.Functions.Execution.Local;
using Elements.Serialization.glTF;
using Elements.Serialization.JSON;
using System.Collections.Generic;


namespace PlansByFloors.Tests
{
    public class PlansByFloorsTests
    {
        [Fact]
        public void PlansByFloorsTest()
        {
            var inputs = new PlansByFloorsInputs(5.0, 20.0, "", "", new Dictionary<string, string>(), "", "", "");
            var model = Model.FromJson(System.IO.File.ReadAllText("../../../../../../TestOutput/FloorsByLevels.json"));
            var outputs = PlansByFloors.Execute(new Dictionary<string, Model>{{"Floors", model}}, inputs);
            System.IO.File.WriteAllText("../../../../../../TestOutput/PlansByFloors.json", outputs.model.ToJson());
            outputs.model.ToGlTF("../../../../../../TestOutput/PlansByFloors.glb");
        }
    }
}
