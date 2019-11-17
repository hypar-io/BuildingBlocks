using Elements;
using Elements.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using GeometryEx;

namespace CoreByLevels
{
    public class CoreMaker
    {
        //const double freightService = 46000.0;
        const double liftService = 4600.0;
        const double occupantLoad = 9.3;
        const double liftSize = 3.0;
        const double stairLength = 6.5;
        const double stairWidth = 4.0;
        const double stairEntry = 3.0;
        const double bathLength = 4.0;
        const double bathWidth = 6.0;
        const double mechLength = 2.0;
        const double mechWidth = 6.0;

        private List<Level> Levels { get; set; }
        private int LiftService { get; set; }
        private Vector3 Position { get; set; }
        private double Rotation { get; set; }       

        public int LiftQuantity { get; set; }
        public List<Room> Restrooms { get; private set; }
        public List<MechanicalCorridor> Mechanicals { get; private set; }
        public List<StairEnclosure> Stairs { get; private set; }
        public List<LiftShaft> Lifts { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="levels"></param>
        /// <param name="rotation"></param>
        public CoreMaker(List<Level> levels, double rotation)
        {
            Levels = new List<Level>();
            Levels.AddRange(levels.OrderBy(l => l.Elevation).ToList());
            Restrooms = new List<Room>();
            Mechanicals = new List<MechanicalCorridor>();
            Stairs = new List<StairEnclosure>();
            Lifts = new List<LiftShaft>();
            Rotation = rotation;
            var corePerim = PlaceCore(Levels.Last().Perimeter);
            if (corePerim == null)
            {
                throw new InvalidOperationException("No valid service core location found.");
            }
            var coreTopo = new TopoBox(corePerim);
            var bathTopo = MakeBaths(coreTopo.W);
            var mechTopo = MakeMech(bathTopo.E);
            var stairTopos = MakeStairs(bathTopo);
            MakeLifts(stairTopos, LiftService);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="shell"></param>
        /// <returns></returns>
        private Polygon PlaceCore(Polygon shell)
        {
            var occLevels = Levels.Where(l => l.Elevation >= 0.0).ToList();
            var occArea = 0.0;
            foreach (var level in occLevels)
            {
                occArea += level.Perimeter.Area();
            }
            var occupants = (int)Math.Ceiling(occArea / occupantLoad);
            LiftQuantity = (int)Math.Floor(occArea / liftService);
            if (LiftQuantity > 8)
            {
                LiftQuantity = 8;
            }
            LiftService = (int)Math.Floor((decimal)occLevels.Count() / LiftQuantity);
            var positions = new List<Vector3> { shell.Centroid() };
            var liftBank = Math.Floor(LiftQuantity * 0.5);
            positions.AddRange(shell.FindInternalPoints((stairLength + (liftBank * liftSize)) * 0.5));
            positions = positions.OrderBy(p => p.DistanceTo(shell.Centroid())).ToList();
            foreach (var position in positions)
            {
                var corePerim = Polygon.Rectangle(stairLength + (liftBank * liftSize),
                                                 (stairWidth * 2) + bathWidth);
                corePerim = corePerim.MoveFromTo(corePerim.Centroid(), position).Rotate(position, Rotation);
                if (shell.Contains(corePerim))
                {
                    Position = position;
                    return corePerim;
                }
            }
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="insertAt"></param>
        /// <returns></returns>
        private TopoBox MakeBaths(Vector3 moveTo)
        {
            var bathPerim = Polygon.Rectangle(bathLength, bathWidth);
            var bathTopo = new TopoBox(bathPerim);
            bathPerim = bathPerim.MoveFromTo(bathTopo.W, moveTo);
            bathTopo = new TopoBox(bathPerim);
            bathPerim = bathPerim.Rotate(Position, Rotation);
            var bathLevels = Levels.Where(l => l.Elevation >= 0.0);
            var bathMatl = new Material(new Color(0.0f, 0.6f, 1.0f, 0.8f), 0.0f, 0.0f, Guid.NewGuid(), "bath");
            
            var i = 0;
            foreach(var level in bathLevels.SkipLast(2))
            {
                var bathHeight = bathLevels.ElementAt(i + 1).Elevation - bathLevels.ElementAt(i).Elevation - 1.0;
                var extrude = new Elements.Geometry.Solids.Extrude(bathPerim, bathHeight, Vector3.ZAxis, 0.0, false);
                var geomRep = new Representation(new List<Elements.Geometry.Solids.SolidOperation>() { extrude });
                Restrooms.Add(new Room(bathPerim, Vector3.ZAxis, Rotation, bathLevels.ElementAt(i).Elevation, 
                                       bathHeight, bathPerim.Area(), "",
                                       new Transform(0.0, 0.0, bathLevels.ElementAt(i).Elevation), bathMatl, geomRep,
                                       Guid.NewGuid(), "Restroom"));
                i++;
            }
            return bathTopo;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="moveTo"></param>
        /// <returns></returns>
        private TopoBox MakeMech(Vector3 moveTo)
        {
            var mechPerim = Polygon.Rectangle(mechLength, mechWidth);
            var mechTopo = new TopoBox(mechPerim);
            mechPerim = mechPerim.MoveFromTo(mechTopo.W, moveTo);
            mechTopo = new TopoBox(mechPerim);
            mechPerim = mechPerim.Rotate(Position, Rotation);
            var lastLevel = Levels.SkipLast(1).Last();
            var mechHeight = lastLevel.Elevation - Levels.First().Elevation;
            var extrude = new Elements.Geometry.Solids.Extrude(mechPerim, mechHeight, Vector3.ZAxis, 0.0, false);
            var geomRep = new Representation(new List<Elements.Geometry.Solids.SolidOperation>() { extrude });
            var mechMatl = new Material(new Color(0.2f, 0.2f, 0.2f, 0.8f), 0.0f, 0.0f, Guid.NewGuid(), "mech");
            var ctr = mechPerim.Centroid();
            Mechanicals.Add(new MechanicalCorridor(mechPerim, Vector3.ZAxis, Rotation,
                                                   new Vector3(ctr.X, ctr.Y, Levels.First().Elevation),
                                                   new Vector3(ctr.X, ctr.Y, Levels.First().Elevation + mechHeight),
                                                   mechHeight, mechPerim.Area() * mechHeight, "",
                                                   new Transform(0.0, 0.0, Levels.First().Elevation),
                                                   mechMatl, geomRep, Guid.NewGuid(), ""));
            return mechTopo;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bathTopo"></param>
        /// <returns></returns>
        private List<TopoBox> MakeStairs(TopoBox bathTopo)
        {
            var stairTopos = new List<TopoBox>();
            for (int i = 0; i < 2; i++)
            {
                Vector3 from = null;
                Vector3 to = null;
                var stairHeight = 0.0;
                var lastLevel = Levels.Last();

                var stairPerim = Polygon.Rectangle(stairLength, stairWidth);
                var stairTopo = new TopoBox(stairPerim);
                if (i == 0)
                {
                    from = stairTopo.SW;
                    to = bathTopo.NW;
                    stairHeight = lastLevel.Elevation - Levels.First().Elevation + stairEntry;
                }
                else
                {
                    from = stairTopo.NW;
                    to = bathTopo.SW;
                    stairHeight = lastLevel.Elevation - Levels.First().Elevation;
                }
                stairPerim = stairPerim.MoveFromTo(from, to);
                stairTopo = new TopoBox(stairPerim);
                stairTopos.Add(stairTopo);
                stairPerim = stairPerim.Rotate(Position, Rotation);
                var extrude = new Elements.Geometry.Solids.Extrude(stairPerim, stairHeight, Vector3.ZAxis, 0.0, false);
                var geomRep = new Representation(new List<Elements.Geometry.Solids.SolidOperation>() { extrude });
                var stairMatl = new Material(new Color(1.0f, 0.0f, 0.0f, 0.8f), 0.0f, 0.0f, Guid.NewGuid(), "stair");
                Stairs.Add(new StairEnclosure(stairPerim, Vector3.ZAxis, Rotation, Levels.First().Elevation,
                                              stairHeight, stairPerim.Area() * stairHeight, "",
                                              new Transform(0.0, 0.0, Levels.First().Elevation),
                                              stairMatl, geomRep, Guid.NewGuid(), ""));
            }
            return stairTopos;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stairs"></param>
        /// <param name="liftSvc"></param>
        private void MakeLifts(List<TopoBox> stairs, int liftSvc)
        {
            var liftMatl = new Material(new Color(1.0f, 0.9f, 0.4f, 0.8f), 0.0f, 0.0f, Guid.NewGuid(), "lift");
            var liftPolys = new List<Polygon>() { Polygon.Rectangle(liftSize, liftSize) };
            for (var i = 0; i < (LiftQuantity * 0.5) - 1; i++)
            {
                var liftPerim = Polygon.Rectangle(liftSize, liftSize);
                var liftTopo = new TopoBox(liftPerim);
                var lastTopo = new TopoBox(liftPolys.Last());
                liftPolys.Add(liftPerim.MoveFromTo(liftTopo.SW, lastTopo.SE));
            }
            var firstTopo = new TopoBox(liftPolys.First());
            var stairTopo = stairs.First();
            var makePolys = new List<Polygon>();
            foreach (var polygon in liftPolys)
            {
                makePolys.Add(polygon.MoveFromTo(firstTopo.SW, stairTopo.SE).Rotate(Position, Rotation));
            }
            liftPolys.Clear();
            liftPolys.Add(Polygon.Rectangle(liftSize, liftSize));
            for (var i = 0; i < (LiftQuantity * 0.5) - 1; i++)
            {
                var liftPerim = Polygon.Rectangle(liftSize, liftSize);
                var liftTopo = new TopoBox(liftPerim);
                var lastTopo = new TopoBox(liftPolys.Last());
                liftPolys.Add(liftPerim.MoveFromTo(liftTopo.SW, lastTopo.SE));
            }
            firstTopo = new TopoBox(liftPolys.First());
            stairTopo = stairs.Last();
            foreach (var polygon in liftPolys)
            {
                
                makePolys.Add(polygon.MoveFromTo(firstTopo.NW, stairTopo.NE).Rotate(Position, Rotation));
            }
            var liftSvcFactor = 0;
            foreach (var polygon in makePolys)
            {
                var lastLevel = Levels.SkipLast((int)liftSvc * liftSvcFactor).Last();
                var liftHeight = lastLevel.Elevation - Levels.First().Elevation;
                if (liftHeight > 10.0)
                {
                    var extrude = new Elements.Geometry.Solids.Extrude(polygon, liftHeight, Vector3.ZAxis, 0.0, false);
                    var geomRep = new Representation(new List<Elements.Geometry.Solids.SolidOperation>() { extrude });
                    Lifts.Add(new LiftShaft(polygon, Vector3.ZAxis, Rotation, Levels.First().Elevation, 
                                            liftHeight, polygon.Area() * liftHeight, "",
                                            new Transform(0.0, 0.0, Levels.First().Elevation), liftMatl, geomRep,
                                            Guid.NewGuid(), ""));

                }
                liftSvcFactor++;
            }
        }
    }
}