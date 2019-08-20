using Xunit;
using System.Linq;
using Newtonsoft.Json;
using Elements;
using Hypar.Functions.Execution.Local;
using Elements.Serialization.glTF;
using Elements.Serialization.JSON;
using System.Collections.Generic;


namespace Story.tests
{
    public class CoreTests
    {
        [Fact]
        public void StoryTest()
        {
            var model = Model.FromJson(System.IO.File.ReadAllText("../../../../core.json"));
            var inputs = new StoryInputs(4.0, 1.0, 8.0, 2.0, "", "", new Dictionary<string, string>(), "", "", "");
            var outputs = Story.Execute(new Dictionary<string, Model>{{"envelope", model}}, inputs);
            System.IO.File.WriteAllText("../../../../storyCore.json", outputs.model.ToJson());
            outputs.model.ToGlTF("../../../../storyCore.glb");

            model = Model.FromJson(System.IO.File.ReadAllText("../../../../mass.json"));
            inputs = new StoryInputs(4.0, 1.0, 8.0, 2.0, "", "", new Dictionary<string, string>(), "", "", "");
            outputs = Story.Execute(new Dictionary<string, Model>{{"envelope", model}}, inputs);
            System.IO.File.WriteAllText("../../../../storyMass.json", outputs.model.ToJson());
            outputs.model.ToGlTF("../../../../storyMass.glb");

            model = Model.FromJson(System.IO.File.ReadAllText("../../../../structureCore.json"));
            inputs = new StoryInputs(4.0, 1.0, 8.0, 2.0, "", "", new Dictionary<string, string>(), "", "", "");
            outputs = Story.Execute(new Dictionary<string, Model>{{"envelope", model}}, inputs);
            System.IO.File.WriteAllText("../../../../storyStructureCore.json", outputs.model.ToJson());
            outputs.model.ToGlTF("../../../../storyStructureCore.glb");
        }
    }
}
