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
            //var payload = JsonConvert.DeserializeObject("{ \"arguments\":{ \"Height\":10,\"Profile\":{ \"Vertices\":[{\"X\":25.26905244272814,\"Y\":-5.81776751389728,\"Z\":-1.2918038891690032e-15},{\"X\":75.74330565562511,\"Y\":1.7218580102887557,\"Z\":-1.3828525433570436e-14},{\"X\":-58.95800137180487,\"Y\":108.82452957809221,\"Z\":9.953044961117865e-15},{\"X\":-72.74516079253712,\"Y\":41.6326339689915,\"Z\":-4.966552953568848e-15}]},\"model_input_keys\":{}}}");
        }
    }
}
