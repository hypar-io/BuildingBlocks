using Xunit;
using System.Linq;
using Newtonsoft.Json;
using Elements;
using Hypar.Functions.Execution.Local;
using Elements.Serialization.glTF;
using Elements.Serialization.JSON;
using System.Collections.Generic;


namespace RoomsByFloors.Tests
{
    public class RoomsByFloorsTests
    {
        [Fact]
        public void RoomsByFloorsTest()
        {
            var inputs = new RoomsByFloorsInputs(2.0, 2.0, 5.0, 5.0, 1.0, 0.0, false, "", "", new Dictionary<string, string>(), "", "", "");
            var model = Model.FromJson(System.IO.File.ReadAllText("../../../../../../TestOutput/~RoomFailure.json"));
            var outputs = RoomsByFloors.Execute(new Dictionary<string, Model>{{"Floors", model}}, inputs);
            System.IO.File.WriteAllText("../../../../../../TestOutput/RoomsByFloors.json", outputs.model.ToJson());
            outputs.model.ToGlTF("../../../../../../TestOutput/RoomsByFloors.glb");
        }
    }
}
