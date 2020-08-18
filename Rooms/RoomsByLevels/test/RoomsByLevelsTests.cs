using Xunit;
using System.Linq;
using Newtonsoft.Json;
using Elements;
using Hypar.Functions.Execution.Local;
using Elements.Serialization.glTF;
using Elements.Serialization.JSON;
using System.Collections.Generic;


namespace RoomsByLevels.Tests
{
    /// <summary>
    /// Writes all new Elements to JSON output.
    /// Writes all new Elements and any incoming contextual Elements to GLB output.
    /// </summary>
    public class RoomsByLevelsTests
    {
        private const string INPUT = "../../../_input/";
        private const string OUTPUT = "../../../_output/";

        [Fact]
        public void RoomsByLevelsArms()
        {
            var inputs = 
                new RoomsByLevelsInputs(
                    corridorWidth: 4.0, 
                    roomArea: 10.0, 
                    "", "", new Dictionary<string, string>(), "", "", "");
            var model = 
                Model.FromJson(System.IO.File.ReadAllText(INPUT + "modelArms.json"));
            var outputs = 
                RoomsByLevels.Execute(new Dictionary<string, Model> { { "Envelope", model }, { "Levels", model } }, inputs);
            System.IO.File.WriteAllText(OUTPUT + "RoomsByLevelsArms.json", outputs.Model.ToJson());
            outputs.Model.AddElements(model.Elements.Values);
            outputs.Model.ToGlTF(OUTPUT + "RoomsByLevelsArms.glb");
        }

        [Fact]
        public void RoomsByLevelsBreak()
        {
            var inputs = new RoomsByLevelsInputs(
                corridorWidth: 2.5, 
                roomArea: 100.0,
                "", "", new Dictionary<string, string>(), "", "", "");
            var model = Model.FromJson(System.IO.File.ReadAllText(INPUT + "modelBreak.json"));
            var outputs = RoomsByLevels.Execute(new Dictionary<string, Model> { { "Envelope", model }, { "Levels", model } }, inputs);
            System.IO.File.WriteAllText(OUTPUT + "RoomsByLevelsBreak.json", outputs.Model.ToJson());
            outputs.Model.AddElements(model.Elements.Values);
            outputs.Model.ToGlTF(OUTPUT + "RoomsByLevelsBreak.glb");
        }

        [Fact]
        public void RoomsByLevelsCross()
        {
            var inputs = new RoomsByLevelsInputs(
                corridorWidth: 2.5,
                roomArea: 100.0,
                "", "", new Dictionary<string, string>(), "", "", "");
            var model = Model.FromJson(System.IO.File.ReadAllText(INPUT + "modelCross.json"));
            var outputs = RoomsByLevels.Execute(new Dictionary<string, Model> { { "Envelope", model }, { "Levels", model } }, inputs);
            System.IO.File.WriteAllText(OUTPUT + "RoomsByLevelsCross.json", outputs.Model.ToJson());
            outputs.Model.AddElements(model.Elements.Values);
            outputs.Model.ToGlTF(OUTPUT + "RoomsByLevelsCross.glb");
        }

        [Fact]
        public void RoomsByLevelsEl()
        {
            var inputs = new RoomsByLevelsInputs(
                corridorWidth: 2.5,
                roomArea: 100.0,
                "", "", new Dictionary<string, string>(), "", "", "");
            var model = Model.FromJson(System.IO.File.ReadAllText(INPUT + "modelEL.json"));
            model.ToGlTF("../../../../test/modelEl.glb");
            var outputs = RoomsByLevels.Execute(new Dictionary<string, Model> { { "Envelope", model }, { "Levels", model } }, inputs);
            System.IO.File.WriteAllText(OUTPUT + "RoomsByLevelsEL.json", outputs.Model.ToJson());
            outputs.Model.AddElements(model.Elements.Values);
            outputs.Model.ToGlTF(OUTPUT + "RoomsByLevelsEL.glb");
        }

        [Fact]
        public void RoomsByLevelsThreeBay()
        {
            var inputs = new RoomsByLevelsInputs(
                corridorWidth: 7.0,
                roomArea: 100.0, 
                "", "", new Dictionary<string, string>(), "", "", "");
            var model = Model.FromJson(System.IO.File.ReadAllText(INPUT + "modelThreeBay.json"));
            var outputs = RoomsByLevels.Execute(new Dictionary<string, Model>{{"Envelope", model}, { "Levels", model } }, inputs);
            System.IO.File.WriteAllText(OUTPUT + "RoomsByLevelsThreeBay.json", outputs.Model.ToJson());
            outputs.Model.AddElements(model.Elements.Values);
            outputs.Model.ToGlTF(OUTPUT + "RoomsByLevelsThreeBay.glb");
        }
    }
}
