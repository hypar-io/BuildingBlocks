using Xunit;
using System.Linq;
using Newtonsoft.Json;
using Elements;
using Elements.Geometry;
using Hypar.Functions.Execution.Local;
using Elements.Serialization.glTF;
using Elements.Serialization.JSON;
using System.Collections.Generic;


namespace PlanByProgram.Tests
{
    /// <summary>
    /// Writes all new Elements to JSON output.
    /// Writes all new Elements and any incoming contextual Elements to GLB output.
    /// </summary>
    public class PlanByProgramTests
    {
        private const string INPUT = "../../../_input/";
        private const string OUTPUT = "../../../_output/";

        [Fact]
        public void AxisTest_Clinic()
        {
            var inputs = 
                new PlanByProgramInputs(suiteRatio: 0.7,
                                        corridorWidth: 3.7,
                                        plenumHeight: 1.5,
                                        multipleLevels: true,
                                        diagonalAdjacency: true,
                                        conformFloorsToRooms: false,
                                        SuitePlanType.Axis,
                                        PrimaryDirection.Northeast,
                                        CoordinateAdjacency.Minimum,
                                        "", "", new Dictionary<string, string>(), "", "", "");
            var model = 
                Model.FromJson(System.IO.File.ReadAllText(INPUT + "ProgramByCSV_Clinic.json"));
            var outputs = 
                PlanByProgram.Execute(new Dictionary<string, Model> { { "Program", model } }, inputs);
            System.IO.File.WriteAllText(OUTPUT + "PlanByProgram-Axis_Clinic.json", outputs.Model.ToJson());
            outputs.Model.AddElements(model.Elements.Values);
            outputs.Model.ToGlTF("../../../../TestOutput/PlanByProgram-Axis_Clinic.glb");
        }

        [Fact]
        public void ReciprocalTest_Clinic()
        {
            var inputs = 
                new PlanByProgramInputs(suiteRatio: 1.5,
                                        corridorWidth: 2.5,
                                        plenumHeight: 0.5,
                                        multipleLevels: true,
                                        diagonalAdjacency: false,
                                        conformFloorsToRooms: false,
                                        SuitePlanType.Reciprocal,
                                        PrimaryDirection.Northeast,
                                        CoordinateAdjacency.Minimum,
                                        "", "", new Dictionary<string, string>(), "", "", "");
            var model = 
                Model.FromJson(System.IO.File.ReadAllText(INPUT + "ProgramByCSV_Clinic.json"));
            var outputs = 
                PlanByProgram.Execute(new Dictionary<string, Model> { { "Program", model } }, inputs);
            System.IO.File.WriteAllText(OUTPUT + "PlanByProgram-Reciprocal_Clinic.json", outputs.Model.ToJson());
            outputs.Model.AddElements(model.Elements.Values);
            outputs.Model.ToGlTF(OUTPUT + "PlanByProgram-Reciprocal_Clinic.glb");
        }

        [Fact]
        public void AxisTest_School()
        {
            var inputs = 
                new PlanByProgramInputs(suiteRatio: 1.5,
                                        corridorWidth: 2.5,
                                        plenumHeight: 1.5,
                                        multipleLevels: true,
                                        diagonalAdjacency: true,
                                        conformFloorsToRooms: true,
                                        SuitePlanType.Axis,
                                        PrimaryDirection.Northeast,
                                        CoordinateAdjacency.Minimum,
                                        "", "", new Dictionary<string, string>(), "", "", "");
            var model = 
                Model.FromJson(System.IO.File.ReadAllText(INPUT + "ProgramByCSV_School.json"));
            var outputs = 
                PlanByProgram.Execute(new Dictionary<string, Model> { { "Program", model } }, inputs);
            System.IO.File.WriteAllText(OUTPUT + "PlanByProgram-Axis_School.json", outputs.Model.ToJson());
            outputs.Model.AddElements(model.Elements.Values);
            outputs.Model.ToGlTF(OUTPUT + "PlanByProgram-Axis_School.glb");
        }

        [Fact]
        public void ReciprocalTest_School()
        {
            var inputs = 
                new PlanByProgramInputs(suiteRatio: 1.5,
                                        corridorWidth: 2.5,
                                        plenumHeight: 1.5,
                                        multipleLevels: true,
                                        diagonalAdjacency: true,
                                        conformFloorsToRooms: true,
                                        SuitePlanType.Reciprocal,
                                        PrimaryDirection.Northeast,
                                        CoordinateAdjacency.Minimum,
                                        "", "", new Dictionary<string, string>(), "", "", "");
            var model = 
                Model.FromJson(System.IO.File.ReadAllText(INPUT + "ProgramByCSV_School.json"));
            var outputs = 
                PlanByProgram.Execute(new Dictionary<string, Model> { { "Program", model } }, inputs);
            System.IO.File.WriteAllText(OUTPUT + "PlanByProgram-Reciprocal_School.json", outputs.Model.ToJson());
            outputs.Model.AddElements(model.Elements.Values);
            outputs.Model.ToGlTF(OUTPUT + "PlanByProgram-Reciprocal_School.glb");
        }
    }
}
