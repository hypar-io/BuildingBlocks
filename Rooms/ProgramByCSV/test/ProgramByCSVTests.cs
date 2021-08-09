using Xunit;
using System.Linq;
using Newtonsoft.Json;
using Elements;
using Hypar.Functions.Execution.Local;
using Elements.Serialization.glTF;
using Elements.Serialization.JSON;
using System.Collections.Generic;


namespace ProgramByCSV.Tests
{
    /// <summary>
    /// Writes all new Elements to JSON output.
    /// </summary>
    public class ProgramByCSVTests
    {
        private const string INPUT = "../../../_input/";
        private const string OUTPUT = "../../../_output/";

        [Fact]
        public void ProgramByCSVTest_Clinic()
        {
            var inputs = 
                new ProgramByCSVInputs(
                    new Hypar.Functions.Execution.InputData(INPUT + "Clinic.csv"),
                                                            useImperialUnits: false,
                                                            suiteSequence: ProgramByCSVInputsSuiteSequence.Listed, 
                                                            roomSequence: ProgramByCSVInputsRoomSequence.Listed,  
                                                            "", "", new Dictionary<string, string>(), "", "", "");
            var outputs = 
                ProgramByCSV.Execute(new Dictionary<string, Model>{ {"Program", new Model() } }, inputs);
            System.IO.File.WriteAllText(OUTPUT + "ProgramByCSV_Clinic.json", outputs.Model.ToJson());
        }

        [Fact]
        public void ProgramByCSVTest_School()
        {
            var inputs = 
                new ProgramByCSVInputs(
                    new Hypar.Functions.Execution.InputData(INPUT + "School.csv"),
                                                            useImperialUnits: false,
                                                            suiteSequence: ProgramByCSVInputsSuiteSequence.Listed,
                                                            roomSequence: ProgramByCSVInputsRoomSequence.Listed,
                                                            "", "", new Dictionary<string, string>(), "", "", "");
            var outputs = 
                ProgramByCSV.Execute(new Dictionary<string, Model> { { "Program", new Model() } }, inputs);
            System.IO.File.WriteAllText(OUTPUT + "ProgramByCSV_School.json", outputs.Model.ToJson());
        }
    }
}
