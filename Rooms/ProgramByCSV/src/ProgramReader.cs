using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace ProgramByCSV
{

    public class ProgramReader
    {
        public struct RoomEntry
        {
            public string suiteName;
            public string suiteNumber;
            public string department;
            public string name;
            public int quantity;
            public double area;
            public double ratio;
            public double height;
        }

        public List<RoomEntry> RoomEntries { get; private set; }
        public double RoomQuantity { get; private set; }
        public double TotalArea { get; private set; }

        /// <summary>
        /// Reads a CSV file room list of the format:
        /// TODO: (enter format)
        /// The first line of the CSV file is assumed to be a header row whose values are ignored. Field meaning is ascribed
        /// by position, and priority of room placement is from top to bottom by file line and grouped by Suite name.
        /// </summary>
        /// <param name="inputs">User inputs.</param>

        public const double METRIC_FACTOR = 0.092903;

        public ProgramReader(ProgramByCSVInputs input)
        {
            RoomEntries = new List<RoomEntry>();
            RoomQuantity = 0.0;
            TotalArea = 0.0;
            var lines = File.ReadAllText(input.Program.LocalFilePath).Split(new char[] { '\n', '\r' });
            if (lines.Count() < 2) //Drop the process if only the header row or an empty file exists.
            {
                return;
            }
            var i = 1;
            foreach (var line in lines.Skip(1)) //skip assumed header row
            {
                var row = new List<string>(line.Trim().Split(','));

                //Skip malformed rows or entries with quantities <= zero;
                if (row.Count() < 8 || !int.TryParse(row.ElementAt(4).ToString(), out int qVal) || qVal <= 0) 
                {
                    continue;
                }
                if (!double.TryParse(row.ElementAt(5).ToString(), out double aVal))
                {
                    throw new ArgumentException("Area in Row " + i.ToString() + " does not resolve to a double value.");
                }
                if (!double.TryParse(row.ElementAt(6).ToString(), out double rVal))
                {
                    throw new ArgumentException("Ratio in Row " + i.ToString() + " does not resolve to a double value.");
                }
                var height = 0.0;
                if (double.TryParse(row.ElementAt(7).ToString(), out double hVal))
                {
                    height = Math.Abs(hVal);
                }
                else
                {
                    height = 4.0;
                }
                var roomEntry = new RoomEntry
                {
                    suiteName = row.ElementAt(0).Trim(),
                    suiteNumber = row.ElementAt(1).Trim(),
                    department = row.ElementAt(2).Trim(),
                    name = row.ElementAt(3).Trim()
                };
                roomEntry.quantity = Math.Abs(qVal);
                roomEntry.area = Math.Abs(aVal);
                roomEntry.height = height;

                if (input.UseImperialUnits) //Convert from Imperial units if indicated.
                {
                    roomEntry.area = Math.Round(roomEntry.area *= METRIC_FACTOR, 3);
                    roomEntry.height = Math.Round(roomEntry.height *= METRIC_FACTOR, 3);
                }
                roomEntry.ratio = Math.Abs(rVal);
                RoomEntries.Add(roomEntry);
                RoomQuantity += roomEntry.quantity;
                TotalArea += roomEntry.area * roomEntry.quantity;
                i++;
            }
            RoomEntrySequence(input);
        }

        private struct SuiteID
        {
            public string Name;
            public string Number;
        }

        private struct SuiteEntry
        {
            public string Name;
            public string Number;
            public List<RoomEntry> Rooms;
            public double Area;
        }

        private void RoomEntrySequence(ProgramByCSVInputs input)
        {
            var suiteIDs = new List<SuiteID>();
            foreach (var roomEntry in RoomEntries)
            {
                suiteIDs.Add(new SuiteID { Name = roomEntry.suiteName, Number = roomEntry.suiteNumber });
            }
            suiteIDs = suiteIDs.Distinct().ToList();
            var suites = new List<SuiteEntry>();
            foreach (var suiteID in suiteIDs)
            {
                var suiteRooms = new List<RoomEntry>();
                var suiteArea = 0.0;
                foreach (var roomEntry in RoomEntries)
                {
                    if (roomEntry.suiteName == suiteID.Name && 
                        roomEntry.suiteNumber == suiteID.Number)
                    {
                        suiteRooms.Add(roomEntry);
                        suiteArea += roomEntry.area * roomEntry.quantity;
                    }
                }
                if (input.RoomSequence == ProgramByCSVInputsRoomSequence.Reverse)
                {
                    suiteRooms.Reverse();
                }
                if (input.RoomSequence == ProgramByCSVInputsRoomSequence.AreaAscending)
                {
                    suiteRooms = suiteRooms.OrderBy(r => r.area).ToList();
                }
                if (input.RoomSequence == ProgramByCSVInputsRoomSequence.AreaDescending)
                {
                    suiteRooms = suiteRooms.OrderByDescending(r => r.area).ToList();
                }
                suites.Add(
                    new SuiteEntry
                    {
                        Name = suiteID.Name,
                        Number = suiteID.Number,
                        Rooms = suiteRooms,
                        Area = suiteArea
                    });
            }
            if (input.SuiteSequence == ProgramByCSVInputsSuiteSequence.Reverse)
            {
                suites.Reverse();
            }
            if (input.SuiteSequence == ProgramByCSVInputsSuiteSequence.AreaAscending)
            {
                suites = suites.OrderBy(s => s.Area).ToList();
            }
            if (input.SuiteSequence == ProgramByCSVInputsSuiteSequence.AreaDescending)
            {
                suites = suites.OrderByDescending(s => s.Area).ToList();
            }
            if (input.SuiteSequence == ProgramByCSVInputsSuiteSequence.NameAscending)
            {
                suites = suites.OrderBy(s => s.Name).ToList();
            }
            if (input.SuiteSequence == ProgramByCSVInputsSuiteSequence.NameDescending)
            {
                suites = suites.OrderByDescending(s => s.Name).ToList();
            }
            if (input.SuiteSequence == ProgramByCSVInputsSuiteSequence.NumberAscending)
            {
                suites = suites.OrderBy(s => s.Number).ToList();
            }
            if (input.SuiteSequence == ProgramByCSVInputsSuiteSequence.NumberDescending)
            {
                suites = suites.OrderByDescending(s => s.Number).ToList();
            }
            RoomEntries.Clear();
            foreach (var suite in suites)
            {
                foreach (var roomEntry in suite.Rooms)
                {
                    RoomEntries.Add(roomEntry);
                }
            }
        }
    }
}