using Xunit;
using Newtonsoft.Json;
using Elements;
using Hypar.Functions.Execution.Local;
using Elements.Serialization.glTF;
using Elements.Serialization.JSON;
using GeometryEx;
using System.Collections.Generic;

namespace Site.tests
{
    public class SiteTests
    {
        private const string feature = @"
        [
          {
            ""geometry"": 
            {
              ""type"": ""Polygon"",
              ""coordinates"": [
                [
                  [
                    -71.092028,
                    42.365714
                  ],
                  [
                    -71.092865,
                    42.364231
                  ],
                  [
                    -71.091449,
                    42.364124
                  ],
                  [
                    -71.091331,
                    42.365111
                  ],
                  [
                    -71.091176,
                    42.365385
                  ],
                  [
                    -71.092028,
                    42.365714
                  ]
                ]
              ]
            },
            ""type"": ""Feature"",
            ""properties"": {
              ""ain"": ""4210033009"",
              ""situsaddr"": ""595 TECHNOLOGY SQUARE"",
              ""situscity"": ""CAMBRIDGE MA"",
              ""situszip_5"": ""02139""
            }
          }
        ]";


        [Fact]
        public void Footprint()
        {
            var mapData = JsonConvert.DeserializeObject<Elements.GeoJSON.Feature[]>(feature);
            var inputs = new SiteInputs(mapData, 3.0, 45.0, 32.0, 5.0, 20.0, "", "", new Dictionary<string, string>(), "", "", "");
            var outputs = Site.Execute(new Dictionary<string, Model>(), inputs);
            outputs.model.ToGlTF("../../../../Site.glb");
            System.IO.File.WriteAllText("../../../../Site.json", outputs.model.ToJson());
        }
    }
}
