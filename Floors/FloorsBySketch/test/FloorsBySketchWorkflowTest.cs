using Elements;
using Xunit;
using System;
using System.IO;
using System.Collections.Generic;
using Elements.Serialization.glTF;
using System.Linq;
using Elements.Geometry;

namespace FloorsBySketch
{
    public class FloorsBySketchWorkflowTest
    {
        [Fact]
        public void OverrideTestExecute()
        {
            var input = GetInput();

            var modelDependencies = new Dictionary<string, Model> { 
            };

            var result = FloorsBySketch.Execute(modelDependencies, input);
            int expectedCountOfPolygons = 6;

            var floors = result.Model.AllElementsOfType<Floor>().OrderBy(f => f.Name).ToList();
            Assert.Equal(expectedCountOfPolygons, floors.Count);

            var testPolygon1 = new Polygon(
                new Vector3(30.0024, -29.8964, -0.3048),
                new Vector3(30.1473, -10.0411, -0.3048),
                new Vector3(0.1557, -29.6785, -0.3048)
            );

            var testPolygon2 = new Polygon(
                new Vector3(-41.5407, -10.5723, -0.3048),
                new Vector3(-31.6954, -20.5971, -0.3048),
                new Vector3(-21.4514, -10.5365, -0.3048),
                new Vector3(-31.2967, -0.5117, -0.3048)
            );

            var testPolygon3 = new Polygon(
                new Vector3(-3.8929, 39.2624, 0.0000),
                new Vector3(-21.9138, 33.7328, 0.0000),
                new Vector3(-35.5169, 9.6098, 0.0000),
                new Vector3(5.7284, 12.1466, 0.0000)
            );

            var testPolygon4 = new Polygon(
                new Vector3(-41.5407, -10.5723, 2.7432),
                new Vector3(-31.6954, -20.5971, 2.7432),
                new Vector3(-21.4514, -10.5365, 2.7432),
                new Vector3(-31.2967, -0.5117, 2.7432)
            );

            var testPolygon5 = new Polygon(
                new Vector3(-41.5407, -10.5723, 5.7912),
                new Vector3(-31.6954, -20.5971, 5.7912),
                new Vector3(-21.4514, -10.5365, 5.7912),
                new Vector3(-31.2967, -0.5117, 5.7912)
            );

            var testPolygon6 = new Polygon(
                new Vector3(-36.2019, -5.3290, 8.8392),
                new Vector3(-36.8357, -15.3631, 8.8392),
                new Vector3(-26.5734, -15.5668, 8.8392),
                new Vector3(-26.3740, -5.5241, 8.8392)
            );

            var expectedPolygons = new Polygon[] { 
                testPolygon1, testPolygon2, testPolygon3, 
                testPolygon4, testPolygon5, testPolygon6 
            };

            for (int i = 0; i < expectedCountOfPolygons; i++)
            {
                var polygon = floors[i].ProfileTransformed().Perimeter;
                var expectedPolygon = expectedPolygons[i];
                Assert.True(expectedPolygon.IsAlmostEqualTo(polygon, 1E-04));
            }

            //result.Model.ToGlTF("../../../FloorsBySketchWorkflowTest/results/FloorsBySketchWorkflowTest.gltf", false);
            //result.Model.ToGlTF("../../../FloorsBySketchWorkflowTest/results/FloorsBySketchWorkflowTest.glb");
            //File.WriteAllText("../../../FloorsBySketchWorkflowTest/results/FloorsBySketchWorkflowTest.json", result.Model.ToJson());

        }

        public FloorsBySketchInputs GetInput()
        {
            var json = File.ReadAllText("../../../FloorsBySketchWorkflowTest/inputs.json");
            return Newtonsoft.Json.JsonConvert.DeserializeObject<FloorsBySketchInputs>(json);
        }
    }
}