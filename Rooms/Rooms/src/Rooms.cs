using Elements;
using Elements.Geometry;
using Elements.Geometry.Solids;
using Elements.Spatial;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Rooms
{
    public static class Rooms
    {
        /// <summary>
        /// The Rooms function.
        /// </summary>
        /// <param name="model">The input model.</param>
        /// <param name="input">The arguments to the execution.</param>
        /// <returns>A RoomsOutputs instance containing computed results and the model with any new elements.</returns>
        public static RoomsOutputs Execute(Dictionary<string, Model> inputModels, RoomsInputs input)
        {
            var walls = inputModels["Walls"].AllElementsOfType<StandardWall>().ToList();
            var network = Elements.Search.Network<StandardWall>.FromSegmentableItems(walls, (wall) => wall.CenterLine, out var allNodeLocations, out var allIntersections);
            var roomCandidates = network.FindAllClosedRegions(allNodeLocations);

            var output = new RoomsOutputs();

            var roomCount = 0;
            var r = new Random(11);

            foreach (var roomCandidate in roomCandidates)
            {
                var roomBoundary = new Polygon(roomCandidate.Select(i => allNodeLocations[i]).ToList());
                var roomRep = new Representation(new List<SolidOperation>(){
                    new Lamina(roomBoundary, null, false)
                });
                var room = new Room(roomBoundary, null, null, null, roomCount, 0.0, 0.0, null, material: r.NextMaterial(), roomRep);
                roomCount++;
                output.Model.AddElement(room);
            }

            return output;
        }
    }
}