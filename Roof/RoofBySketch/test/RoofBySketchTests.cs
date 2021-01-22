using Xunit;
using System.Linq;
using Newtonsoft.Json;
using Elements;
using Hypar.Functions.Execution.Local;
using Elements.Serialization.glTF;
using Elements.Serialization.JSON;
using System.Collections.Generic;
using Elements.Geometry;


namespace RoofBySketch.Tests
{
    /// <summary>
    /// Writes all new Elements to JSON output.
    /// Writes all new Elements and any incoming contextual Elements to GLB output.
    /// </summary>
    public class RoofBySketchTests
    {
        private const string OUTPUT = "../../../_output/";

        [Fact]
        public void RoofBySketchTest()
        {
            IList<Vertices> vertices =
                new List<Vertices>
                {
                    new Vertices(0, true, new Vector3(2.0, 2.0, 12.0)),
                    new Vertices(1, true, new Vector3(9.0, 2.0, 12.0)),
                    new Vertices(2, true, new Vector3(9.0, 13.0, 12.0)),
                    new Vertices(3, true, new Vector3(2.0, 13.0, 12.0)),
                    new Vertices(4, false, new Vector3(5.0, 4.0, 14.0)),
                    new Vertices(5, false, new Vector3(5.0, 11.0, 14.0))
                };
            IList<Triangles> triangles =
                new List<Triangles>
                {
                    new Triangles(new List<int> { 0, 1, 4}),
                    new Triangles(new List<int> { 1, 2, 5}),
                    new Triangles(new List<int> { 2, 3, 5}),
                    new Triangles(new List<int> { 3, 0, 4}),
                    new Triangles(new List<int> { 4, 5, 3}),
                    new Triangles(new List<int> { 1, 5, 4})
                };
            var mesh = new Mesh();// triangles, vertices, BuiltInMaterials.Concrete);
            var inputs =
                new RoofBySketchInputs(
                    mesh: mesh,
                    thickness: 0.2,
                    "", "", new Dictionary<string, string>(), "", "", "");
            var outputs = RoofBySketch.Execute(new Dictionary<string, Model> { { "Test", new Model() } }, inputs);
            System.IO.File.WriteAllText(OUTPUT + "RoofBySketch.json", outputs.Model.ToJson());
            //outputs.Model.ToGlTF(OUTPUT + "RoofBySketch.glb");
        }
    }
}
