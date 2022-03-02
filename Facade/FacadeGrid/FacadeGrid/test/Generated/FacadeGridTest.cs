
// This code was generated by Hypar.
// Edits to this code will be overwritten the next time you run 'hypar test generate'.
// DO NOT EDIT THIS FILE.

using Elements;
using Xunit;
using System.IO;
using System.Collections.Generic;
using Elements.Serialization.glTF;

namespace FacadeGrid
{
    public class FacadeGridTest
    {
        [Fact]
        public void TestExecute()
        {
            var input = GetInput();

            var modelDependencies = new Dictionary<string, Model> { 
                {"Envelope", Model.FromJson(File.ReadAllText(@"/Users/andrewheumann/Dev/BuildingBlocks/Facade/FacadeGrid/FacadeGrid/test/Generated/FacadeGridTest/model_dependencies/Envelope/5bea23a9-e0df-4ba3-96b8-ef3592c0a2f7.json")) }, 
                {"Levels", Model.FromJson(File.ReadAllText(@"/Users/andrewheumann/Dev/BuildingBlocks/Facade/FacadeGrid/FacadeGrid/test/Generated/FacadeGridTest/model_dependencies/Levels/d87cd853-dee5-43c9-acff-0659b87e74a4.json")) }, 
            };

            var result = FacadeGrid.Execute(modelDependencies, input);
            result.Model.ToGlTF("../../../Generated/FacadeGridTest/results/FacadeGridTest.gltf", false);
            result.Model.ToGlTF("../../../Generated/FacadeGridTest/results/FacadeGridTest.glb");
            File.WriteAllText("../../../Generated/FacadeGridTest/results/FacadeGridTest.json", result.Model.ToJson());
        }

        public FacadeGridInputs GetInput()
        {
            var json = File.ReadAllText("../../../Generated/FacadeGridTest/inputs.json");
            return Newtonsoft.Json.JsonConvert.DeserializeObject<FacadeGridInputs>(json);
        }
    }
}