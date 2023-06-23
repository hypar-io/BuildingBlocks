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

        private List<LevelPerimeter> Levels { get; set; }
        private int LiftService { get; set; }
        private Vector3 Position { get; set; }
        private double Rotation { get; set; }

        public int LiftQuantity { get; set; }
        public List<Room> Restrooms { get; private set; }
        public List<MechanicalCorridor> Mechanicals { get; private set; }
        public List<StairEnclosure> Stairs { get; private set; }
        public List<LiftShaft> Lifts { get; private set; }
        public Polygon Perimeter { get; private set; }
        public double Elevation { get; private set; }

        /// <summary>
        ///
        /// </summary>
        /// <param name="levels"></param>
        /// <param name="rotation"></param>
        public CoreMaker(List<LevelPerimeter> levels, double setback, double rotation)
        {
            Levels = new List<LevelPerimeter>();
            Levels.AddRange(levels.OrderBy(l => l.Elevation).ToList());
            Restrooms = new List<Room>();
            Mechanicals = new List<MechanicalCorridor>();
            Stairs = new List<StairEnclosure>();
            Lifts = new List<LiftShaft>();
            Rotation = rotation;
            var corePerim = PlaceCore(setback, rotation);
            Perimeter = corePerim;
            Elevation = Levels.First().Elevation;
            var coreTopo = new CompassBox(corePerim);
            var bathTopo = MakeBaths(coreTopo.W);
            var mechTopo = MakeMech(bathTopo.E);
            var stairTopos = MakeStairs(bathTopo);
            MakeLifts(stairTopos, LiftService);

            //Following section for debug.
            //Comment for deployment.

            //Mechanicals.Clear();
            //var ctr = corePerim.Centroid();
            //var lastLevel = Levels.Last();
            //var mechHeight = lastLevel.Elevation - Levels.First().Elevation + 5.0;
            //var extrude = new Elements.Geometry.Solids.Extrude(corePerim, mechHeight, Vector3.ZAxis, 0.0, false);
            //var geomRep = new Representation(new List<Elements.Geometry.Solids.SolidOperation>() { extrude });
            //var mechMatl = new Material(new Color(0.2f, 0.2f, 0.2f, 0.8f), 0.0f, 0.0f, Guid.NewGuid(), "mech");
            //Mechanicals.Add(new MechanicalCorridor(corePerim, Vector3.ZAxis, Rotation,
            //                                       new Vector3(ctr.X, ctr.Y, Levels.First().Elevation),
            //                                       new Vector3(ctr.X, ctr.Y, Levels.First().Elevation + mechHeight),
            //                                       mechHeight, corePerim.Area() * mechHeight, "",
            //                                       new Transform(0.0, 0.0, Levels.First().Elevation),
            //                                       mechMatl, geomRep, Guid.NewGuid(), ""));


        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="shell"></param>
        /// <returns></returns>
        private Polygon PlaceCore(double setback, double rotation)
        {
            var offShells = Levels.Last().Perimeter.Offset(setback * -1);
            if (offShells.Count() == 0)
            {
                return null;
            }
            var shell = offShells.OrderByDescending(s => s.Area()).First();
            var occLevels = Levels.Where(l => l.Elevation >= 0.0).ToList();
            var occArea = 0.0;
            foreach (var level in occLevels)
            {
                occArea += level.Perimeter.Area();
            }
            if (occArea > 0)
            {
                var occupants = (int)Math.Ceiling(occArea / occupantLoad);
                LiftQuantity = (int)Math.Ceiling(occArea / liftService);
                if (LiftQuantity > 8)
                {
                    LiftQuantity = 8;
                }
                LiftService = (int)Math.Floor((decimal)occLevels.Count() / LiftQuantity);
            }
            var positions = new List<Vector3> { shell.Centroid() };
            var liftBank = Math.Floor(LiftQuantity * 0.5);
            positions.AddRange(shell.FindInternalPoints((stairLength + (liftBank * liftSize)) * 0.5));
            positions = positions.OrderBy(p => p.DistanceTo(shell.Centroid())).ToList();
            foreach (var position in positions)
            {
                var corePerim = Polygon.Rectangle(stairLength + (liftBank * liftSize),
                                                 (stairWidth * 2) + bathWidth);
                corePerim = corePerim.MoveFromTo(corePerim.Centroid(), position).Rotate(position, Rotation);
                if (shell.Covers(corePerim))
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
        private CompassBox MakeBaths(Vector3 moveTo)
        {
            var bathPerim = Polygon.Rectangle(bathLength, bathWidth);
            var bathTopo = new CompassBox(bathPerim);
            bathPerim = bathPerim.MoveFromTo(bathTopo.W, moveTo);
            bathTopo = new CompassBox(bathPerim);
            bathPerim = bathPerim.Rotate(Position, Rotation);
            var bathLevels = Levels.Where(l => l.Elevation >= 0.0);
            var bathMatl = new Material()
            {
                Name = "bath",
                Color = new Color(0.0, 0.6, 1.0, 0.8),
                SpecularFactor = 0.0,
                GlossinessFactor = 0.0
            };

            var i = 0;
            foreach (var level in bathLevels.SkipLast(2))
            {
                var bathHeight = bathLevels.ElementAt(i + 1).Elevation - bathLevels.ElementAt(i).Elevation - 1.0;
                var extrude = new Elements.Geometry.Solids.Extrude(bathPerim, bathHeight, Vector3.ZAxis, false);
                var geomRep = new Representation(new List<Elements.Geometry.Solids.SolidOperation>() { extrude });
                Restrooms.Add(new Room
                {
                    Perimeter = bathPerim,
                    Direction = Vector3.ZAxis,
                    SuiteName = "",
                    SuiteNumber = "",
                    Department = "",
                    Number = "",
                    DesignArea = 0.0,
                    DesignRatio = 0.0,
                    Rotation = Rotation,
                    LevelName = bathLevels.ElementAt(i).Name,
                    Elevation = bathLevels.ElementAt(i).Elevation,
                    Height = bathHeight,
                    Area = bathPerim.Area(),
                    Transform = new Transform(0.0, 0.0, bathLevels.ElementAt(i).Elevation),
                    Material = bathMatl,
                    Representation = geomRep,
                    Name = "Restroom"
                });
                i++;
            }
            return bathTopo;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="moveTo"></param>
        /// <returns></returns>
        private CompassBox MakeMech(Vector3 moveTo)
        {
            var suitableLevels = Levels.SkipLast(1);
            // we need at least two floors to build a mesh between them
            if (suitableLevels.Count() < 2)
            {
                return null;
            }

            var mechPerim = Polygon.Rectangle(mechLength, mechWidth);
            var mechTopo = new CompassBox(mechPerim);
            mechPerim = mechPerim.MoveFromTo(mechTopo.W, moveTo);
            mechTopo = new CompassBox(mechPerim);
            mechPerim = mechPerim.Rotate(Position, Rotation);
            var lastLevel = suitableLevels.Last();
            var mechHeight = lastLevel.Elevation - Levels.First().Elevation;
            var extrude = new Elements.Geometry.Solids.Extrude(mechPerim, mechHeight, Vector3.ZAxis, false);
            var geomRep = new Representation(new List<Elements.Geometry.Solids.SolidOperation>() { extrude });
            var mechMatl = new Material()
            {
                Name = "mech",
                Color = new Color(0.2, 0.2, 0.2, 0.8),
                SpecularFactor = 0.0,
                GlossinessFactor = 0.0
            };
            var ctr = mechPerim.Centroid();
            Mechanicals.Add(new MechanicalCorridor(mechPerim, Vector3.ZAxis, Rotation,
                                                   new Vector3(ctr.X, ctr.Y, Levels.First().Elevation),
                                                   new Vector3(ctr.X, ctr.Y, Levels.First().Elevation + mechHeight),
                                                   mechHeight, mechPerim.Area() * mechHeight, "",
                                                   new Transform(0.0, 0.0, Levels.First().Elevation),
                                                   mechMatl, geomRep, false, Guid.NewGuid(), ""));
            return mechTopo;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="bathTopo"></param>
        /// <returns></returns>
        private List<CompassBox> MakeStairs(CompassBox bathTopo)
        {
            var stairTopos = new List<CompassBox>();
            var stairsCount = Levels.Count() > 1 ? 2 : 1;
            for (int i = 0; i < stairsCount; i++)
            {
                Vector3 from;
                Vector3 to;
                var stairHeight = 0.0;
                var lastLevel = Levels.Last();

                var stairPerim = Polygon.Rectangle(stairLength, stairWidth);
                var stairTopo = new CompassBox(stairPerim);
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
                stairTopo = new CompassBox(stairPerim);
                stairTopos.Add(stairTopo);
                stairPerim = stairPerim.Rotate(Position, Rotation);
                var extrude = new Elements.Geometry.Solids.Extrude(stairPerim, stairHeight, Vector3.ZAxis, false);
                var geomRep = new Representation(new List<Elements.Geometry.Solids.SolidOperation>() { extrude });
                var stairMatl = new Material()
                {
                    Name = "stair",
                    Color = new Color(1.0, 0.0, 0.0, 0.8),
                    SpecularFactor = 0.0,
                    GlossinessFactor = 0.0
                };
                Stairs.Add(new StairEnclosure(stairPerim, Vector3.ZAxis, Rotation, Levels.First().Elevation,
                                              stairHeight, stairPerim.Area() * stairHeight, "",
                                              new Transform(0.0, 0.0, Levels.First().Elevation),
                                              stairMatl, geomRep, false, Guid.NewGuid(), ""));
            }
            return stairTopos;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="stairs"></param>
        /// <param name="liftSvc"></param>
        private void MakeLifts(List<CompassBox> stairs, int liftSvc)
        {
            var liftMatl = new Material()
            {
                Name = "lift",
                Color = new Color(1.0, 0.9, 0.4, 0.8),
                SpecularFactor = 0.0,
                GlossinessFactor = 0.0
            };

            var liftPolys = new List<Polygon>() { Polygon.Rectangle(liftSize, liftSize) };
            for (var i = 0; i < (LiftQuantity * 0.5) - 1; i++)
            {
                var liftPerim = Polygon.Rectangle(liftSize, liftSize);
                var liftTopo = new CompassBox(liftPerim);
                var lastTopo = new CompassBox(liftPolys.Last());
                liftPolys.Add(liftPerim.MoveFromTo(liftTopo.SW, lastTopo.SE));
            }
            var firstTopo = new CompassBox(liftPolys.First());
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
                var liftTopo = new CompassBox(liftPerim);
                var lastTopo = new CompassBox(liftPolys.Last());
                liftPolys.Add(liftPerim.MoveFromTo(liftTopo.SW, lastTopo.SE));
            }
            firstTopo = new CompassBox(liftPolys.First());
            stairTopo = stairs.Last();
            foreach (var polygon in liftPolys)
            {

                makePolys.Add(polygon.MoveFromTo(firstTopo.NW, stairTopo.NE).Rotate(Position, Rotation));
            }
            var liftSvcFactor = 0;
            foreach (var polygon in makePolys)
            {
                var suitableLevels = Levels.SkipLast((int)liftSvc * liftSvcFactor);
                if (!suitableLevels.Any())
                {
                    break;
                }

                var lastLevel = suitableLevels.Last();
                var liftHeight = lastLevel.Elevation - Levels.First().Elevation;
                if (liftHeight > 10.0)
                {
                    var extrude = new Elements.Geometry.Solids.Extrude(polygon, liftHeight, Vector3.ZAxis, false);
                    var geomRep = new Representation(new List<Elements.Geometry.Solids.SolidOperation>() { extrude });
                    Lifts.Add(new LiftShaft(polygon, Vector3.ZAxis, Rotation, Levels.First().Elevation,
                                            liftHeight, polygon.Area() * liftHeight, "",
                                            new Transform(0.0, 0.0, Levels.First().Elevation), liftMatl, geomRep,
                                            false, Guid.NewGuid(), ""));
                }
                liftSvcFactor++;
            }
        }
    }
}