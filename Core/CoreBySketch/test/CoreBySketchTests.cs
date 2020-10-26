using Xunit;
using System.Linq;
using Newtonsoft.Json;
using Elements;
using Elements.Geometry;
using Hypar.Functions.Execution.Local;
using Elements.Serialization.glTF;
using Elements.Serialization.JSON;
using System.Collections.Generic;


namespace CoreBySketch.tests
{
    /// <summary>
    /// Writes all new Elements to JSON output.
    /// Writes all new Elements and any incoming contextual Elements to GLB output.
    /// </summary>
    public class CoreBySketchTests
    {
        private const string INPUT = "../../../_input/";
        private const string OUTPUT = "../../../_output/";

        [Fact]
        public void CoreBySketchTest()
        {
            var polygon =
                new Polygon
                (
                    new[]
                    {
                        new Vector3(5.0, 5.0),
                        new Vector3(10.0, 5.0),
                        new Vector3(10.0, 10.0),
                        new Vector3(5.0, 10.0)
                    }
                );
            var model = 
                Model.FromJson(System.IO.File.ReadAllText(INPUT + "LevelsByEnvelope.json"));
            var inputs = 
                new CoreBySketchInputs(
                    perimeter: polygon, 
                    coreHeightAboveRoof: 3.0, 
                    "", "", new Dictionary<string, string>(), "", "", "");
            var outputs = 
                CoreBySketch.Execute(new Dictionary<string, Model>{{"Levels", model}}, inputs);
            System.IO.File.WriteAllText(OUTPUT + "CoreBySketch.json", outputs.Model.ToJson());
            outputs.Model.AddElements(model.Elements.Values);
            outputs.Model.ToGlTF(OUTPUT + "CoreBySketch.glb");
        }
    }
}
