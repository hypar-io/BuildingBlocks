using Xunit;
using System.Linq;
using Newtonsoft.Json;
using Elements;
using Hypar.Functions.Execution.Local;
using Elements.Serialization.glTF;
using Elements.Serialization.JSON;
using System.Collections.Generic;


namespace ColumnsByFloors.Tests
{
    public class ColumnsByFloorsTests
    {
        [Fact]
        public void ColumnsByFloorsTest()
        {
            var inputs = new ColumnsByFloorsInputs(4.0, 5.0, 15.0, 0.5, "", "", new Dictionary<string, string>(), "", "", "");
            var model = Model.FromJson(System.IO.File.ReadAllText("../../../../../../TestOutput/FloorsByLevels.json"));
            var outputs = ColumnsByFloors.Execute(new Dictionary<string, Model>{{"Floors", model}}, inputs);
            System.IO.File.WriteAllText("../../../../../../TestOutput/ColumnsByFloors.json", outputs.model.ToJson());
            outputs.model.ToGlTF("../../../../../../TestOutput/ColumnsByFloors.glb");
        }

        [Fact]
        public void MultipleBuildingsTest()
        {
            var inputs = new ColumnsByFloorsInputs(4.0, 5.0, 15.0, 0.5, "", "", new Dictionary<string, string>(), "", "", "");
            var model = Model.FromJson(System.IO.File.ReadAllText("../../../../../../TestOutput/GHFloorsByLevels.json"));
            var outputs = ColumnsByFloors.Execute(new Dictionary<string, Model> { { "Floors", model } }, inputs);
            System.IO.File.WriteAllText("../../../../../../TestOutput/ColumnsInMultipleBuildings.json", outputs.model.ToJson());
            outputs.model.ToGlTF("../../../../../../TestOutput/ColumnsInMultipleBuildings.glb");
        }
    }
}
