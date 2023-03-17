using Elements;
using Elements.Geometry;
using System;
using System.Collections.Generic;

namespace ProgramByCSV
{
    public static class ProgramByCSV
    {
        /// <summary>
        /// The ProgramByCSV function.
        /// </summary>
        /// <param name="model">The input model.</param>
        /// <param name="input">The arguments to the execution.</param>
        /// <returns>A ProgramByCSVOutputs instance containing computed results and the model with any new elements.</returns>
        public static ProgramByCSVOutputs Execute(Dictionary<string, Model> inputModels, ProgramByCSVInputs input)
        {
            if (input.Program.Key == null || input.Program.LocalFilePath == null) return new ProgramByCSVOutputs();

            var roomReader = new ProgramReader(input);
            var output = new ProgramByCSVOutputs(roomReader.RoomEntries.Count, roomReader.RoomQuantity, roomReader.TotalArea);
            foreach (var roomEntry in roomReader.RoomEntries)
            {
                output.Model.AddElement(new RoomDefinition(roomEntry.suiteName,
                                                           roomEntry.suiteNumber,
                                                           roomEntry.department,
                                                           roomEntry.name,
                                                           roomEntry.quantity,
                                                           roomEntry.area,
                                                           roomEntry.ratio,
                                                           roomEntry.height,
                                                           Guid.NewGuid(),
                                                           roomEntry.name));
            }
            return output;
        }
    }
}