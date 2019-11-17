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
                        new Vector3(100.0, 0.0),
                        new Vector3(100.0, 100.0),
                        new Vector3(60.0, 100.0),
                        new Vector3(60.0, 30.0),
                        new Vector3(40.0, 40.0),
                        new Vector3(40.0, 100.0),
                        new Vector3(0.0, 100.0)
                    }
                );
            var inputs = 
                new EnvelopeBySketchInputs (polygon, 80.0, 20.0, 2.0, 100.0, 20.0, "", "", new Dictionary<string, string>(), "", "", "");
            var outputs = EnvelopeBySketch.Execute(new Dictionary<string, Model> { { "Envelope", model } }, inputs);
            System.IO.File.WriteAllText("../../../../../../TestOutput/EnvelopeBySketch.json", outputs.model.ToJson());
            outputs.model.ToGlTF("../../../../../../TestOutput/EnvelopeBySketch.glb");
        }
    }
}
