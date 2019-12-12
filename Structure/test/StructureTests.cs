using Xunit;
using System.Linq;
using Newtonsoft.Json;
using Elements;
using Hypar.Functions.Execution.Local;
using Elements.Serialization.glTF;
using Elements.Serialization.JSON;
using System.Collections.Generic;


namespace Structure.tests
{
    public class StructureTests
    {
        [Fact]
        public void StructureTest()
        {
            var envModel = Model.FromJson(System.IO.File.ReadAllText("../../../../../TestOutput/EnvelopeBySketch.json"));
            var lvlModel = Model.FromJson(System.IO.File.ReadAllText("../../../../../TestOutput/LevelsByEnvelope.json"));
            var inputs = new StructureInputs(10.0, 10.0, 15.5, false, "", "", new Dictionary<string, string>(), "", "", "");
            var outputs = Structure.Execute(new Dictionary<string, Model> { { "Envelope", envModel }, { "Levels", lvlModel } }, inputs);
            System.IO.File.WriteAllText("../../../../../TestOutput/structure.json", outputs.model.ToJson());
            outputs.model.ToGlTF("../../../../../TestOutput/structure.glb");
            
            // Partial Structure result:
            var model = Model.FromJson(System.IO.File.ReadAllText("../../../../../TestOutput/structureProblem.json"));
            inputs = new StructureInputs(8.0, 7.0, 18.0, false, "", "", new Dictionary<string, string>(), "", "", "");
            outputs = Structure.Execute(new Dictionary<string, Model> { { "Envelope", model }, { "Levels", model }  }, inputs);
            System.IO.File.WriteAllText("../../../../../TestOutput/tructureFail.json", outputs.model.ToJson());
            outputs.model.ToGlTF("../../../../../TestOutput/structureFail.glb");
        }
    }
}
