using Elements;
using Elements.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using GeometryEx;
using RoomKit;

namespace PlansByFloors
{
    public static class PlansByFloors
    {
        /// <summary>
        /// The PlansByFloors function.
        /// </summary>
        /// <param name="model">The input model.</param>
        /// <param name="input">The arguments to the execution.</param>
        /// <returns>A PlansByFloorsOutputs instance containing computed results and the model with any new elements.</returns>
        public static PlansByFloorsOutputs Execute(Dictionary<string, Model> inputModels, PlansByFloorsInputs input)
        {
            var floors = new List<Floor>();
            inputModels.TryGetValue("Floors", out var model);
            floors.AddRange(model.AllElementsOfType<Floor>());
 
            floors = floors.OrderBy(f => f.Elevation).Where(f => f.Elevation >= 0.0).ToList();
            var upperFloorArea = 0.0;
            foreach (var floor in floors.Skip(1).SkipLast(2))
            {
                upperFloorArea += floor.Profile.Perimeter.Area();
            }
            var output = new PlansByFloorsOutputs(input.GroundFloorRooms,
                                                  floors.First().Profile.Perimeter.Area(),
                                                  input.TypicalRoomsPerUpperFloor,
                                                  input.TypicalRoomsPerUpperFloor * (floors.Count() - 3),
                                                  upperFloorArea);
            var retlColor = Palette.Emerald;
            retlColor.Alpha = 1.0;
            var offcColor = Palette.Cobalt;
            offcColor.Alpha = 1.0;
            var corrColor = Palette.White;
            corrColor.Alpha = 1.0;
            var retlMatl = new Material("retail", retlColor, 0.0f, 0.0f);
            var offcMatl = new Material("office", offcColor, 0.0f, 0.0f);
            var corrMatl = new Material("corridor", corrColor, 0.0f, 0.0f);
            for (var i = 0; i < floors.Count() - 2; i++)
            {
                var floor = floors.ElementAt(i);
                var ceiling = floors.ElementAt(i + 1);
                var perimeter = floor.Profile.Perimeter;
                var perimBox = new TopoBox(perimeter);

                var height = ceiling.Elevation - ceiling.Thickness - floor.Elevation;
                var story = new Story(floor.Elevation, height, false, "", perimeter);
                if (i == 0)
                {
                    story.RoomsByDivision((int)input.GroundFloorRooms, 1, height, 0.0, "", Palette.Green, true);
                }
                else
                {
                    story.RoomsByDivision((int)Math.Floor(input.TypicalRoomsPerUpperFloor * 0.5), 1, height, 0.0, "");
                    //var corPerim = Polygon.Rectangle(perimBox.SizeX, 2.0);
                    //var corBox = new TopoBox(corPerim);
                    //corPerim = corPerim.MoveFromTo(corBox.W, perimBox.W);
                    //var corridor = new RoomKit.Room("", "", 0.0, 0.0, floor.Elevation, height, corPerim);
                    //story.AddCorridor(corridor, true, true);
                }
                foreach (var room in story.Rooms)
                {
                    var name = "Office";
                    var matl = offcMatl;
                    if (room.Elevation == 0.0)
                    {
                        name = "Retail";
                        matl = retlMatl;
                    }
                    var panel = room.Perimeter.Offset(-0.15).First();
                    var lamina = new Elements.Geometry.Solids.Lamina(panel, false);
                    var geomRep = new Representation(new List<Elements.Geometry.Solids.SolidOperation>() { lamina });
                    output.model.AddElement(new Elements.Room(panel, Vector3.ZAxis, 0.0,
                                                              room.Elevation, room.Height, room.Area, "",
                                                              new Transform(0.0, 0.0, room.Elevation),
                                                              matl, geomRep, Guid.NewGuid(), name));
                }
                //foreach (var room in story.Corridors)
                //{
                //    var panel = room.Perimeter.Offset(0.05).First();
                //    var lamina = new Elements.Geometry.Solids.Lamina(panel, false);
                //    var geomRep = new Representation(new List<Elements.Geometry.Solids.SolidOperation>() { lamina });
                //    output.model.AddElement(new Elements.Room(panel, Vector3.ZAxis, 0.0,
                //                                              room.Elevation + 0.05, room.Height, room.Area, "",
                //                                              new Transform(0.0, 0.0, room.Elevation),
                //                                              corrMatl, geomRep, Guid.NewGuid(), "Corridor"));
                //}
            }
            return output;
        }
    }
}