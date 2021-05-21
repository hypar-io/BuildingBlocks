using Xunit;
using Hypar.Functions.Execution;
using System.Threading.Tasks;
using Elements;
using Elements.Geometry;
using Elements.Serialization.glTF;
using Xunit.Abstractions;
using Hypar.Functions.Execution.Local;
using System.Collections.Generic;


namespace SubdivideSlab.Tests
{
    /// <summary>
    /// Writes all new Elements to JSON output.
    /// Writes all new Elements and any incoming contextual Elements to GLB output.
    /// </summary>
    public class SubDivideSlabTests
    {
        private const string INPUT = "../../../_input/";
        private const string OUTPUT = "../../../_output/";

        [Fact]
        public void SubdivideSlabImportFloor()
        {
            var model = 
                Model.FromJson(System.IO.File.ReadAllText(INPUT + "Floors.json"));
            var inputs = 
                new SubdivideSlabInputs(
                    length: 200.0, 
                    width: 200.0, 
                    subdivideAtVoidCorners: false, 
                    alignToLongestEdge: true, 
                    "", "", new Dictionary<string, string>(), "", "", "");
            var outputs = 
                SubdivideSlab.Execute(new Dictionary<string, Model> { { "Floors", model } }, inputs);
            System.IO.File.WriteAllText(OUTPUT + "SubdivideSlabImportFloor.json", outputs.Model.ToJson());
            outputs.Model.AddElements(model.Elements.Values);
            outputs.Model.ToGlTF(OUTPUT + "SubdivideSlabImportFloor.glb");
        }

        [Fact]
        public void SubdivideSlabRotated()
        {
            var polygon = new Polygon(new[] {
                new Vector3(0.0, 0.0),
                new Vector3(5.0, 0.0),
                new Vector3(10.0, 5.0),
                new Vector3(5.0, 10.0)
            });
            var model = new Model();
            model.AddElement(new Floor(polygon, 2.0));
            var inputs = new SubdivideSlabInputs(
                    length: 2.0,
                    width: 2.0,
                    subdivideAtVoidCorners: false,
                    alignToLongestEdge: true,
                    "", "", new Dictionary<string, string>(), "", "", "");
            var outputs = SubdivideSlab.Execute(new Dictionary<string, Model> { { "Floors", model } }, inputs);
            System.IO.File.WriteAllText(OUTPUT + "SubdivideSlabRotated.json", outputs.Model.ToJson());
            outputs.Model.AddElements(model.Elements.Values);
            outputs.Model.ToGlTF(OUTPUT + "SubdivideSlabRotated.glb");
        }

    }
}