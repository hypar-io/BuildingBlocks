
// This code was generated by Hypar.
// Edits to this code will be overwritten the next time you run 'hypar test generate'.
// DO NOT EDIT THIS FILE.

using Elements;
using Xunit;
using System;
using System.IO;
using System.Collections.Generic;
using Elements.Serialization.glTF;

namespace People
{
    public class PeopleTest
    {
        [Fact]
        public void TestExecute()
        {
            var input = GetInput();

            var modelDependencies = new Dictionary<string, Model> { 
                {"Floors", Model.FromJson(File.ReadAllText(@"/Users/ikeough/Documents/Hypar/EntourageScatterer/test/Generated/PeopleTest/model_dependencies/Floors/d741fdff-e163-4c48-afef-c1c3d85b4745.json")) }, 
            };

            var result = People.Execute(modelDependencies, input);
            result.Model.ToGlTF("../../../Generated/PeopleTest/results/PeopleTest.gltf", false);
            result.Model.ToGlTF("../../../Generated/PeopleTest/results/PeopleTest.glb");
            File.WriteAllText("../../../Generated/PeopleTest/results/PeopleTest.json", result.Model.ToJson());

        }

        public PeopleInputs GetInput()
        {
            var json = File.ReadAllText("../../../Generated/PeopleTest/inputs.json");
            return Newtonsoft.Json.JsonConvert.DeserializeObject<PeopleInputs>(json);
        }
    }
}