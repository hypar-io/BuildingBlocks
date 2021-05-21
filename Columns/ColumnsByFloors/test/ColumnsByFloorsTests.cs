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
    /// <summary>
    /// Writes all new Elements to JSON output.
    /// Writes all new Elements and any incoming contextual Elements to GLB output.
    /// </summary>

    public class ColumnsByFloorsTests
    {
        private const string INPUT = "../../../_input/";
        private const string OUTPUT = "../../../_output/";

        [Fact]
        public void ColumnsByFloorsTest()
        {
            var inputs = 
                new ColumnsByFloorsInputs(
                    gridXAxisInterval: 15.0, 
                    gridYAxisInterval: 15.0, 
                    gridRotation: 20.0, 
                    columnDiameter: 0.5, 
                    "", "", new Dictionary<string, string>(), "", "", "");
            var model = Model.FromJson(System.IO.File.ReadAllText(INPUT + "FloorsByLevels.json"));
            var outputs = ColumnsByFloors.Execute(new Dictionary<string, Model>{{"Floors", model}}, inputs);
            System.IO.File.WriteAllText(OUTPUT + "ColumnsByFloors.json", outputs.Model.ToJson());
            outputs.Model.AddElements(model.Elements.Values);
            outputs.Model.ToGlTF(OUTPUT + "ColumnsByFloors.glb");
        }
    }
}
