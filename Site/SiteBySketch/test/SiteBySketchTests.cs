using Xunit;
using System.Linq;
using Newtonsoft.Json;
using Elements;
using Hypar.Functions.Execution.Local;
using Elements.Serialization.glTF;
using Elements.Serialization.JSON;
using System.Collections.Generic;
using Elements.Geometry;


namespace SiteBySketch.Tests
{
    public class SiteBySketchTests
    {
        [Fact]
        public void SiteBySketchTest()
        {
            var model = new Model();
            var polygon =
                new Polygon(
                    new[]
                    {
                        new Vector3(-46.0, -29.0, 0.0),
                        new Vector3(-10.0, -43.0, -0.0),
                        new Vector3(33.0, -40.0, -0.0),
                        new Vector3(36.0, 71.0, 0.0)
                    });
            var inputs = 
                new SiteBySketchInputs (polygon, "", "", new Dictionary<string, string>(), "", "", "");
            var outputs = SiteBySketch.Execute(new Dictionary<string, Model> { { "Test", model } }, inputs);
            System.IO.File.WriteAllText("../../../../../../TestOutput/SiteBySketch.json", outputs.model.ToJson());
            outputs.model.ToGlTF("../../../../../../TestOutput/SiteBySketch.glb");
        }
    }
}
