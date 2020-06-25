using Xunit;
using Hypar.Functions.Execution;
using System.Threading.Tasks;
using Elements;
using Elements.Geometry;
using Xunit.Abstractions;
using Hypar.Functions.Execution.Local;
using System.Collections.Generic;
using System.IO;

namespace SubdivideSlab.Tests
{
    public class RunTest
    {
        [Fact]
        public void RunSubdivAlgo()
        {
            var jsonIn = System.IO.File.ReadAllText("../../../../Floors.json");
            var model = Model.FromJson(jsonIn);
            var inputs = new SubdivideSlabInputs(2, 2, false, true, "", "", new Dictionary<string, string>(), "", "", "");
            var outputs = SubdivideSlab.Execute(new Dictionary<string, Model> { { "Floors", model } }, inputs);
            var json = outputs.Model.ToJson();

        }

        [Fact]
        public void TestRotation()
        {
            var polygon = new Polygon(new[] {
                new Vector3(0,0,0),
                new Vector3(5,0,0),
                new Vector3(10,5,0),
                new Vector3(5,10,0)
            });
            var profile = new Profile(polygon);
            var floor = new Floor(profile, 2);
            var model = new Model();
            model.AddElement(floor);
            var inputs = new SubdivideSlabInputs(2, 2, false, true, "", "", new Dictionary<string, string>(), "", "", "");

            var outputs = SubdivideSlab.Execute(new Dictionary<string, Model> { { "Floors", model } }, inputs);
            var json = outputs.Model.ToJson();
        }

    }
}