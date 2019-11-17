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
    public class CoreByLevelsTests
    {
        [Fact]
        public void CoreByLevelsTest()
        {
            var model = Model.FromJson(System.IO.File.ReadAllText("../../../../../../TestOutput/LevelsByEnvelope.json"));
            var inputs = new CoreByLevelsInputs(1.0, 45.0, "", "", new Dictionary<string, string>(), "", "", "");
            var outputs = CoreByLevels.Execute(new Dictionary<string, Model>{{"Levels", model}}, inputs);
            System.IO.File.WriteAllText("../../../../../../TestOutput/CoreByLevels.json", outputs.model.ToJson());
            outputs.model.ToGlTF("../../../../../../TestOutput/CoreByLevels.glb");
        }
    }
}
