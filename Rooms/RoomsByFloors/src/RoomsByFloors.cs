using Elements;
using Elements.Geometry;
using System.Collections.Generic;
using System;
using System.Linq;
using GeometryEx;

namespace RoomsByFloors
{
      public static class RoomsByFloors
    {
        /// <summary>
        /// Creates Rooms on each Floor supplied by another function..
        /// </summary>
        /// <param name="model">The input model.</param>
        /// <param name="input">The arguments to the execution.</param>
        /// <returns>A RoomsByFloorsOutputs instance containing computed results and the model with any new elements.</returns>
        public static RoomsByFloorsOutputs Execute(Dictionary<string, Model> inputModels, RoomsByFloorsInputs input)
        {
            var floors = new List<Floor>();
            inputModels.TryGetValue("Floors", out var model);
            if (model == null)
            {
                throw new ArgumentException("No Floors found.");
            }
            floors.AddRange(model.AllElementsOfType<Floor>());

            floors = floors.OrderBy(f => f.Elevation).Where(f => f.Elevation >= 0.0).ToList();
            var upperFloorArea = 0.0;
            foreach (var floor in floors.Skip(1).SkipLast(2))
            {
                upperFloorArea += floor.Profile.Perimeter.Area();
            }

            var retlColor = Palette.Emerald;
            var offcColor = Palette.Cobalt;
            var retlMatl = new Material("retail", retlColor, 0.0f, 0.0f);
            var offcMatl = new Material("office", offcColor, 0.0f, 0.0f);

            var rooms = new List<Room>();
            var grdRooms = 0;
            var grdArea = 0.0;
            var upArea = 0.0;
            var typRooms = 0;
            var typRoomCount = 0;

            for (var i = 0; i < floors.Count() - 2; i++)
            {
                var floor = floors.ElementAt(i);
                var ceiling = floors.ElementAt(i + 1);
                var height = ceiling.ProfileTransformed().Perimeter.Vertices.First().Z
                             - floor.ProfileTransformed().Perimeter.Vertices.First().Z
                             - floor.Thickness - 0.7;
                var offPerims = ceiling.Profile.Perimeter.Offset(input.PlanSetback * -1);
                if (offPerims.Count() == 0)
                {
                    throw new InvalidOperationException("Plan Setback too deep. No valid room boundaries could be created.");
                }
                var perimeter = offPerims.First();
                var perimBox = new TopoBox(perimeter);

                var xDiv = 1.0;
                var yDiv = 1.0;

                var xRoomSize = 1.0;
                var yRoomSize = 1.0;

                var roomColor = offcColor;

                if (i == 0)
                {
                    xDiv = input.GroundFloorRoomLengthDivisions;
                    yDiv = input.GroundFloorRoomWidthDivisions;
                    xRoomSize = (int)Math.Ceiling(perimBox.SizeX / xDiv);
                    yRoomSize = (int)Math.Ceiling(perimBox.SizeY / yDiv);
                    grdRooms = (int)Math.Floor(xDiv * yDiv);
                    grdArea = perimeter.Area();
                    roomColor = retlColor;
                }
                else
                {
                    xDiv = input.TypicalFloorRoomsLengthDivisions;
                    yDiv = input.TypicalFloorRoomsWidthDivisions;
                    xRoomSize = (int)Math.Ceiling(perimBox.SizeX / xDiv);
                    yRoomSize = (int)Math.Ceiling(perimBox.SizeY / yDiv);
                    typRooms = (int)Math.Floor(xDiv * yDiv);
                    typRoomCount += typRooms;
                    upArea += perimeter.Area();
                }

                var perimeters = new List<Polygon>();
                var loc = perimBox.SW;

                for (var x = 0; x < xDiv; x++)
                {
                    for (var y = 0; y < yDiv; y++)
                    {
                        var perim = Polygon.Rectangle(xRoomSize, yRoomSize);
                        var pTopo = new TopoBox(perim);
                        perimeters.Add(perim.MoveFromTo(pTopo.SW, loc));
                        loc = new Vector3(loc.X, loc.Y + yRoomSize, 0.0);
                    }
                    loc = new Vector3(loc.X + xRoomSize, perimBox.SW.Y, 0.0);
                }

                foreach (var perim in perimeters)
                {
                    var name = "Office";
                    var matl = offcMatl;
                    if (i == 0)
                    {
                        name = "Retail";
                        matl = retlMatl;
                    }
                    var rPerim = perim.Rotate(floor.Profile.Perimeter.Centroid(), input.PlanRotation);
                    if (!rPerim.Intersects(perimeter))
                    {
                        continue;
                    }
                    var rmPerims = perimeter.Intersection(rPerim);
                    foreach (var rmPerim in rmPerims)
                    {
                        var roomPerims = rmPerim.Offset(-0.2);
                        if (roomPerims.Count() == 0)
                        {
                            continue;
                        }
                        foreach (var room in roomPerims)
                        {
                            Representation geomRep = null;
                            if (input.RoomsIn3D)
                            {
                                var solid = new Elements.Geometry.Solids.Extrude(room, height, Vector3.ZAxis, 0.0, false);
                                geomRep = new Representation(new List<Elements.Geometry.Solids.SolidOperation>() { solid });
                            }
                            else
                            {
                                var solid = new Elements.Geometry.Solids.Lamina(room, false);
                                geomRep = new Representation(new List<Elements.Geometry.Solids.SolidOperation>() { solid });
                            }
                            var rm = new Room(room, Vector3.ZAxis, 0.0, floor.Elevation, height, room.Area(), "",
                                              new Transform(floor.Transform), matl, geomRep, Guid.NewGuid(), name);
                            rm.Transform.Move(new Vector3(0.0, 0.0, 0.7));
                            rooms.Add(rm);
                        }
                    }
                }
            }

            var output = new RoomsByFloorsOutputs(grdRooms, grdArea, typRooms, typRoomCount, upArea);
            foreach (var room in rooms)
            {
                output.model.AddElement(room);
            }
            return output;
        }
    }
}