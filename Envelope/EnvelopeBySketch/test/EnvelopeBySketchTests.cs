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
                new Polygon(
                    new[]
                    {
                        new Vector3(-46.00204011207204, -29.460602642549897, -1.3646995232219328e-14),
                        new Vector3(-10.225898701360123, -43.56593477123123, -2.56815341686672e-15),
                        new Vector3(33.14919014927673, -40.0512850750256, -8.893171771223863e-15),
                        new Vector3(36.211581252868946, 70.98100041683477, 2.2866375552340562e-14)
                    });
            var inputs = new EnvelopeBySketchInputs(polygon, 100.0, 10.0, 30.0, 2.0, "", "", new Dictionary<string, string>(), "", "", "");
            var outputs = EnvelopeBySketch.Execute(new Dictionary<string, Model> { { "site", model } }, inputs);
            System.IO.File.WriteAllText("../../../../Envelope.json", outputs.model.ToJson());
            var store = new FileModelStore<Hypar.Functions.Execution.ArgsBase>("../../../../", true);
            outputs.model.ToGlTF("../../../../Envelope.glb");
        }
    }
}
