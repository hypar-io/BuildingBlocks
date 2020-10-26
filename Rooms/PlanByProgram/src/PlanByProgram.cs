using System;
using System.Linq;
using Elements;
using Elements.Geometry;
using System.Collections.Generic;
using GeometryEx;
using RoomKit;

namespace PlanByProgram
{
    public static class PlanByProgram
    {
        /// <summary>
        /// The PlanByProgram function.
        /// </summary>
        /// <param name="model">The input model.</param>
        /// <param name="input">The arguments to the execution.</param>
        /// <returns>A PlanByProgramOutputs instance containing computed results and the model with any new elements.</returns>
        public static PlanByProgramOutputs Execute(Dictionary<string, Model> inputModels, PlanByProgramInputs input)
        {
            var rmDefModel = inputModels["Program"];
            var rmDefs = new List<RoomDefinition>(rmDefModel.AllElementsOfType<RoomDefinition>().ToList());
            if (rmDefs.Count == 0)
            {
                throw new ArgumentException("No Program found.");
            }
            var suites = Placer.PlaceSuites(input, rmDefs);
            var tmpput = new PlanByProgramOutputs();
            var roomCount = 0;
            var roomArea = 0.0;
            foreach (var suite in suites)
            {
                roomCount += suite.Rooms.Count;
                foreach (var room in suite.Rooms)
                {
                    var extrude = new Elements.Geometry.Solids.Extrude(new Profile(room.Perimeter),
                                                                       room.Height,
                                                                       Vector3.ZAxis,
                                                                       false);
                    var geomRep = new Representation(new List<Elements.Geometry.Solids.SolidOperation>() { extrude });
                    tmpput.Model.AddElement(new Elements.Room(room.Perimeter,
                                                              Vector3.ZAxis,
                                                              room.Suite,
                                                              room.SuiteID,
                                                              room.Department,
                                                              room.Number,
                                                              room.DesignArea,
                                                              room.DesignRatio,
                                                              0.0,
                                                              room.Elevation,
                                                              room.Height,
                                                              room.Area,
                                                              new Transform(),
                                                              room.ColorAsMaterial,
                                                              geomRep,
                                                              false,
                                                              Guid.NewGuid(),
                                                              room.Name));
                    roomArea += room.Area;
                }
            }
            var suitesByElevation = suites.GroupBy(s => s.Elevation);
            var totalArea = 0.0;
            foreach (var group in suitesByElevation)
            {
                var elevation = group.First().Elevation;
                var suiteprints = new List<Polygon>();
                foreach (var suite in group)
                {
                    if (input.ConformFloorsToRooms)
                    {
                        var roomPrints = Shaper.Merge(suite.RoomsAsPolygons);
                        if (roomPrints.Count == 0)
                        {
                            suiteprints.Add(suite.CompassCorridor.Box.Offset(input.CorridorWidth).First());
                        }
                        foreach (var roomPrint in roomPrints)
                        {
                            suiteprints.Add(roomPrint.Offset(input.CorridorWidth).First());
                        }
                    }
                    else
                    {
                        suiteprints.Add(suite.CompassCorridor.Box.Offset(input.CorridorWidth).First());
                    }
                }
                suiteprints = Shaper.Merge(suiteprints);
                foreach(var footprint in suiteprints)
                {
                    tmpput.Model.AddElement(new Floor(footprint, 0.1, new Transform(0.0, 0.0, elevation - 0.1),
                                                      BuiltInMaterials.Concrete, null, false, Guid.NewGuid(), ""));
                    tmpput.Model.AddElement(new Level(elevation, Guid.NewGuid(), elevation.ToString()));
                    totalArea += footprint.Area();
                }
            }
            var output = new PlanByProgramOutputs(roomCount, roomArea, totalArea - roomArea, totalArea)
            {
                Model = tmpput.Model
            };
            return output;
        }
    }
}