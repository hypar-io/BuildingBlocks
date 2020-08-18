using System;
using System.Linq;
using System.Collections.Generic;
using Elements;
using Elements.Geometry;
using Elements.Serialization.glTF;
using GeometryEx;
using RoomKit;

namespace RoomsByLevels
{
    public static class RoomsByLevels
    {
        /// <summary>
        /// Generates Rooms of a supplied area on each incoming Level deployed along the Level's straight skeleton spine which is used as a central corridor path.
        /// </summary>
        /// <param name="model">The input model.</param>
        /// <param name="input">The arguments to the execution.</param>
        /// <returns>A RoomsByLevelsOutputs instance containing computed results and the model with any new elements.</returns>
        public static RoomsByLevelsOutputs Execute(Dictionary<string, Model> inputModels, RoomsByLevelsInputs input)
        {
            var levels = new List<LevelPerimeter>();
            inputModels.TryGetValue("Levels", out var lvlModel);
            if (lvlModel != null)
            {
                levels.AddRange(lvlModel.AllElementsOfType<LevelPerimeter>());
            }
            if (levels.Count == 0)
            {
                throw new ArgumentException("No LevelPerimeters found.");
            }
            levels = levels.OrderBy(l => l.Elevation).ToList();
            var stories = new List<Story>();
            for (var i = 0; i < levels.Count - 1; i++)
            {
                var perimeter = levels[i].Perimeter;
                var elevation = levels[i].Elevation;
                var height = (levels[i + 1].Elevation - levels[i].Elevation) - 0.5; // replace this number with a plenum height
                var spine = levels[i].Perimeter.Spine();
                var sections = levels[i].Perimeter.Jigsaw();
                var endSects = new List<Polygon>();
                var midSects = new List<Polygon>();
                var corridors = new List<Polygon>();
                var story =
                    new Story(perimeter)
                    {
                        Elevation = elevation
                    };
                foreach (var line in spine)
                {
                    corridors.Add(line.ToPolyline().Offset(input.CorridorWidth * 0.5, EndType.Square).First());
                }
                corridors = Shaper.Merge(corridors);
                foreach (var corridor in corridors)
                {
                    story.AddCorridor(new RoomKit.Room(corridor.Straighten(input.CorridorWidth * 0.1).Simplify(input.CorridorWidth * 0.9), height), false);
                }
                foreach (var polygon in sections)
                {
                    if (polygon.Vertices.Count == 3)
                    {
                        endSects.Add(polygon.AlignedBox());
                        continue;
                    }
                    midSects.Add(polygon);
                }
                var scaffolds = new List<Polygon>(endSects);
                foreach (var midSect in midSects)
                {
                    scaffolds.AddRange(Shaper.Differences(midSect.ToList(), endSects));
                }
                var roomRows = new List<RoomRow>();
                foreach (var polygon in scaffolds)
                {
                    roomRows.Add(new RoomRow(polygon));
                }
                foreach (var row in roomRows)
                {
                    var result = row.AddRoomByArea(row.Area * 0.5, height, elevation);
                    result = row.AddRoomByArea(row.Area * 0.5, height, elevation);
                    row.Infill(height, true);
                }
                var rowRooms = new List<RoomKit.Room>();
                foreach (var row in roomRows)
                {
                    rowRooms.AddRange(row.Rooms);
                }
                foreach (var room in rowRooms)
                {
                    story.AddRoom(room);
                }
                stories.Add(story);
            }
            var rooms = new List<Elements.Room>();
            foreach (var story in stories)
            {
                foreach (var room in story.Rooms)
                {
                    room.Color = Palette.Aqua;
                    var solid = new Elements.Geometry.Solids.Extrude(room.Perimeter, room.Height, Vector3.ZAxis, false);
                    var geomRep = new Representation(new List<Elements.Geometry.Solids.SolidOperation>() { solid });
                    rooms.Add(new Elements.Room(room.Perimeter,
                                                Vector3.ZAxis,
                                                "", "", "", "",
                                                input.RoomArea,
                                                room.Ratio,
                                                0.0,
                                                room.Elevation,
                                                room.Height,
                                                room.Area,
                                                null,
                                                room.ColorAsMaterial,
                                                geomRep,
                                                false,
                                                Guid.NewGuid(),
                                                room.Name));
                }
                foreach (var room in story.Corridors)
                {
                    room.Color = Palette.White;
                    var solid = new Elements.Geometry.Solids.Extrude(room.Perimeter, room.Height, Vector3.ZAxis, false);
                    var geomRep = new Representation(new List<Elements.Geometry.Solids.SolidOperation>() { solid });
                    rooms.Add(new Elements.Room(room.Perimeter,
                                                Vector3.ZAxis,
                                                "", "", "", "",
                                                input.RoomArea,
                                                room.Ratio,
                                                0.0,
                                                room.Elevation,
                                                room.Height,
                                                room.Area,
                                                null,
                                                room.ColorAsMaterial,
                                                geomRep,
                                                false,
                                                Guid.NewGuid(),
                                                room.Name));
                }
            }           
            var output = new RoomsByLevelsOutputs(rooms.Count / levels.Count, rooms.Count);
            foreach (var room in rooms)
            {
                output.Model.AddElement(room);
            }
            return output;
        }
    }
}