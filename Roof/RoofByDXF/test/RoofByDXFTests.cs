using Xunit;
using System.Linq;
using Newtonsoft.Json;
using Elements;
using Hypar.Functions.Execution.Local;
using Elements.Serialization.glTF;
using Elements.Serialization.JSON;
using System.Collections.Generic;


namespace RoofByDXF.Tests
{
    /// <summary>
    /// Writes all new Elements to JSON output.
    /// </summary>
    public class RoofByDXFTests
    {
        private const string INPUT = "../../../_input/";
        private const string OUTPUT = "../../../_output/";

        [Fact]
        public void RoofByDXF1()
        {
            var inputs = 
                new RoofByDXFInputs(
                    new Hypar.Functions.Execution.InputData(INPUT + "Roof1.dxf"),
                                                            roofElevation: 20.0,
                                                            roofThickness: 0.1,
                                                            "", "", new Dictionary<string, string>(), "", "", "");
            var outputs =
                RoofByDXF.Execute(new Dictionary<string, Model>{ {"Program", new Model() } }, inputs);
            System.IO.File.WriteAllText(OUTPUT + "RoofByDXF1.json", outputs.Model.ToJson());
            outputs.Model.ToGlTF(OUTPUT + "RoofByDXF1.glb");
        }

        [Fact]
        public void RoofByDXF2()
        {
            var inputs =
                new RoofByDXFInputs(
                    new Hypar.Functions.Execution.InputData(INPUT + "Roof2.dxf"),
                                                            roofElevation: 20.0,
                                                            roofThickness: 0.1,
                                                            "", "", new Dictionary<string, string>(), "", "", "");
            var outputs =
                RoofByDXF.Execute(new Dictionary<string, Model> { { "Program", new Model() } }, inputs);
            System.IO.File.WriteAllText(OUTPUT + "RoofByDXF2.json", outputs.Model.ToJson());
            outputs.Model.ToGlTF(OUTPUT + "RoofByDXF2.glb");
        }

        [Fact]
        public void RoofByDXF3()
        {
            var inputs =
                new RoofByDXFInputs(
                    new Hypar.Functions.Execution.InputData(INPUT + "Roof3.dxf"),
                                                            roofElevation: 20.0,
                                                            roofThickness: 0.1,
                                                            "", "", new Dictionary<string, string>(), "", "", "");
            var outputs =
                RoofByDXF.Execute(new Dictionary<string, Model> { { "Program", new Model() } }, inputs);
            System.IO.File.WriteAllText(OUTPUT + "RoofByDXF3.json", outputs.Model.ToJson());
            outputs.Model.ToGlTF(OUTPUT + "RoofByDXF3.glb");
        }
    }
}
