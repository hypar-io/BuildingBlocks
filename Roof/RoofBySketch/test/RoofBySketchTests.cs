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
            // IList<Vertices> vertices =
            //     new List<Vertices>
            //     {
            //         new Vertices(0, true, new Vector3(2.0, 2.0, 12.0)),
            //         new Vertices(1, true, new Vector3(9.0, 2.0, 12.0)),
            //         new Vertices(2, true, new Vector3(9.0, 13.0, 12.0)),
            //         new Vertices(3, true, new Vector3(2.0, 13.0, 12.0)),
            //         new Vertices(4, false, new Vector3(5.0, 4.0, 14.0)),
            //         new Vertices(5, false, new Vector3(5.0, 11.0, 14.0))
            //     };
            // IList<Triangles> triangles =
            //     new List<Triangles>
            //     {
            //         new Triangles(new List<int> { 0, 1, 4}),
            //         new Triangles(new List<int> { 1, 2, 5}),
            //         new Triangles(new List<int> { 2, 3, 5}),
            //         new Triangles(new List<int> { 3, 0, 4}),
            //         new Triangles(new List<int> { 4, 5, 3}),
            //         new Triangles(new List<int> { 1, 5, 4})
            //     };
            // var mesh = new Mesh();// triangles, vertices, BuiltInMaterials.Concrete);
            // var inputs =
            //     new RoofBySketchInputs(
            //         mesh: mesh,
            //         thickness: 0.2,
            //         "", "", new Dictionary<string, string>(), "", "", "");
            var inputs = GetInputsFromJSON();
            var outputs = RoofBySketch.Execute(new Dictionary<string, Model> { { "Test", new Model() } }, inputs);
            System.IO.File.WriteAllText(OUTPUT + "RoofBySketch.json", outputs.Model.ToJson());
            var model = Model.FromJson(System.IO.File.ReadAllText(OUTPUT + "RoofBySketch.json"), out var errors);
            Assert.Empty(errors);
            outputs.Model.ToGlTF(OUTPUT + "RoofBySketch.gltf", false);
        }

        private RoofBySketchInputs GetInputsFromJSON()
        {
            return JsonConvert.DeserializeObject<RoofBySketchInputs>(@"
{""Thickness"":0.39,""Mesh"":{""triangles"":[{""vertexIndices"":[9,1,0]},{""vertexIndices"":[8,9,0]},{""vertexIndices"":[6,4,7]},{""vertexIndices"":[6,5,4]},{""vertexIndices"":[7,4,9]},{""vertexIndices"":[4,2,9]},{""vertexIndices"":[7,9,8]},{""vertexIndices"":[3,2,4]}],""vertices"":[{""isBoundary"":true,""index"":0,""position"":{""X"":-32.080017042764055,""Y"":32.03334390433628,""Z"":1}},{""isBoundary"":true,""index"":1,""position"":{""X"":33.423251524014645,""Y"":39.23780177057091,""Z"":1}},{""isBoundary"":true,""index"":2,""position"":{""X"":15.807296362839068,""Y"":-32.68103445339828,""Z"":1}},{""isBoundary"":true,""index"":3,""position"":{""X"":-24.48142991150595,""Y"":-37.05336641145442,""Z"":1}},{""isBoundary"":true,""index"":4,""position"":{""X"":-27.22638002663017,""Y"":-12.09615131882644,""Z"":1}},{""isBoundary"":true,""index"":5,""position"":{""X"":-43.441245042013975,""Y"":-13.879563267054944,""Z"":1}},{""isBoundary"":true,""index"":6,""position"":{""X"":-41.56417651142919,""Y"":5.720059694044522,""Z"":1}},{""isBoundary"":true,""index"":7,""position"":{""X"":-29.55875718726762,""Y"":9.109931135800398,""Z"":1}},{""isBoundary"":true,""index"":8,""position"":{""X"":-30.596259617236147,""Y"":18.542951853073788,""Z"":-0.1}},{""isBoundary"":true,""index"":9,""position"":{""X"":29.257487533375436,""Y"":22.230668438920418,""Z"":-0.1}}]},""overrides"":""__vue_devtool_undefined__""}
            ", new MeshConverter());
        }
    }
}
