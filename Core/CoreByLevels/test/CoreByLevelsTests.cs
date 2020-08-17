using Xunit;
using System;
using System.Linq;
using Newtonsoft.Json;
using Elements;
using Elements.Geometry;
using Hypar.Functions.Execution.Local;
using Elements.Serialization.glTF;
using Elements.Serialization.JSON;
using System.Collections.Generic;


namespace CoreByLevels.tests
{
    /// <summary>
    /// Writes all new Elements to JSON output.
    /// Writes all new Elements and any incoming contextual Elements to GLB output.
    /// </summary>
    public class CoreByLevelsTests
    {
        private const string INPUT = "../../../_input/";
        private const string OUTPUT = "../../../_output/";

        [Fact]
        public void CoreByLevelsTest()
        {
            var model = 
                Model.FromJson(System.IO.File.ReadAllText(INPUT + "LevelsByEnvelope.json"));
            var inputs = 
                new CoreByLevelsInputs(
                    setback: 1.0, 
                    rotation: 45.0, 
                    "", "", new Dictionary<string, string>(), "", "", "");
            var outputs = 
                CoreByLevels.Execute(new Dictionary<string, Model>{{"Levels", model}}, inputs);
            System.IO.File.WriteAllText(OUTPUT + "CoreByLevels.json", outputs.Model.ToJson());
            outputs.Model.AddElements(model.Elements.Values);
            outputs.Model.ToGlTF(OUTPUT + "CoreByLevels.glb");
        }

        [Fact]
        public void CoreBuildingTest()
        {
            var model = Model.FromJson(System.IO.File.ReadAllText(INPUT + "Building-01.json"));
            var inputs =
                new CoreByLevelsInputs(
                    setback: 1.0,
                    rotation: 45.0,
                    "", "", new Dictionary<string, string>(), "", "", "");
            var outputs = 
                CoreByLevels.Execute(new Dictionary<string, Model> { { "Levels", model } }, inputs);
            System.IO.File.WriteAllText(OUTPUT + "CoreBuilding.json", outputs.Model.ToJson());
            outputs.Model.AddElements(model.Elements.Values);
            outputs.Model.ToGlTF(OUTPUT + "CoreBuilding.glb");
        }

    }
}
