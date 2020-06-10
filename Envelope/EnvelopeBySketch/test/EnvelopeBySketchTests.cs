using Xunit;
using System.Linq;
using Newtonsoft.Json;
using Elements;
using Hypar.Functions.Execution.Local;
using Elements.Serialization.glTF;
using Elements.Serialization.JSON;
using System.Collections.Generic;
using Elements.Geometry;


namespace EnvelopeBySketch.Tests
{
    public class EnvelopeBySketchTests
    {
        [Fact]
        public void EnvelopeBySketchTest()
        {
            var model = new Model();
            var polygon =
                new Polygon
                (
                    new[]
                    {
                        new Vector3(0.0, 0.0),
                        new Vector3(20.0, 0.0),
                        new Vector3(20.0, 20.0),
                        new Vector3(0.0, 20.0)
                    }
                );
            var inputs =
                new EnvelopeBySketchInputs(polygon, 27.0, 50.0, 5.0, 100.0, 3.0, "", "", new Dictionary<string, string>(), "", "", "");
            var outputs = EnvelopeBySketch.Execute(new Dictionary<string, Model> { { "Envelope", model } }, inputs);
            System.IO.File.WriteAllText("../../../../../../TestOutput/EnvelopeBySketch.json", outputs.Model.ToJson());
            outputs.Model.ToGlTF("../../../../../../TestOutput/EnvelopeBySketch.glb");
        }
    }
}
