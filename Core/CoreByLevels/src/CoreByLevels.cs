using Elements;
using Elements.Geometry;
using System;
using System.Linq;
using System.Collections.Generic;
using GeometryEx;

namespace CoreByLevels
{
    public static class CoreByLevels
    {
        /// <summary>
        /// The CoreByLevels function.
        /// </summary>
        /// <param name="model">The input model.</param>
        /// <param name="input">The arguments to the execution.</param>
        /// <returns>A CoreByLevelsOutputs instance containing computed results and the model with any new elements.</returns>
        public static CoreByLevelsOutputs Execute(Dictionary<string, Model> inputModels, CoreByLevelsInputs input)
        {
            const double liftService = 4600.0;
            //const double freightService = 46000.0;
            const double occupantLoad = 9.3;
            const double liftSize = 3.0;
            const double stairLength = 6.5;
            const double stairWidth = 4.0;
            const double stairEntry = 3.0;
            const double bathLength = 4.0;
            const double bathWidth = 6.0;
            const double mechLength = 2.0;
            const double mechWidth = 6.0;

            var bathColor = new Color(0.0f, 0.6f, 1.0f, 0.8f);
            var mechColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            var stairColor = new Color(1.0f, 0.0f, 0.0f, 0.8f);
            var liftColor = new Color(1.0f, 0.9f, 0.4f, 0.8f);

            var levels = new List<Level>();
            inputModels.TryGetValue("Levels", out var model);
            levels.AddRange(model.AllElementsOfType<Level>());
            levels = levels.OrderBy(l => l.Elevation).ToList();
            var occLevels = levels.Where(l => l.Elevation >= 0.0).ToList();
            var occArea = 0.0;
            foreach (var level in occLevels)
            {
                occArea += level.Perimeter.Area();
            }
            var occupants = (int)Math.Ceiling(occArea / occupantLoad);
            var liftQty = Math.Floor(occArea / liftService);
            var liftSvc = Math.Floor(occLevels.Count() / liftQty);
            var liftBank = Math.Floor(liftQty / 2);
            //var frtQty = Math.Floor(occArea / freightService);


            var position = levels.First().Perimeter.Centroid();
            var coreLength = stairLength + (liftBank * liftSize);
            var coreWidth = (stairWidth * 2) + bathWidth;
            var corePerim = Polygon.Rectangle(coreLength, coreWidth);
            corePerim = corePerim.MoveFromTo(corePerim.Centroid(), position);

            var coreTopo = new TopoBox(corePerim);
            var coreBase = levels.First().Elevation;
            var coreHeight = Math.Abs(coreBase) + levels.Last().Elevation + stairEntry;

            var output = new CoreByLevelsOutputs(0.0);

            var bathPerim = Polygon.Rectangle(bathLength, bathWidth);
            var bathTopo = new TopoBox(bathPerim);
            bathPerim = bathPerim.MoveFromTo(bathTopo.W, coreTopo.W);
            bathTopo = new TopoBox(bathPerim);
            var bathLevels = levels.Where(l => l.Elevation >= 0.0).SkipLast(1);
            var lastLevel = bathLevels.Last();
            var bathHeight = lastLevel.Elevation - bathLevels.First().Elevation;
            var bathMatl = new Material(bathColor, 0.0f, 0.0f, Guid.NewGuid(), "bath");
            var bathMass = new Mass(new Profile(bathPerim), bathHeight, bathMatl,
                                    new Transform(0.0, 0.0, bathLevels.First().Elevation),
                                    null, Guid.NewGuid(), "");

            var mechPerim = Polygon.Rectangle(mechLength, mechWidth);
            var mechTopo = new TopoBox(mechPerim);
            mechPerim = mechPerim.MoveFromTo(mechTopo.W, bathTopo.E);
            mechTopo = new TopoBox(mechPerim);
            lastLevel = levels.SkipLast(1).Last();
            var mechHeight = lastLevel.Elevation - levels.First().Elevation;
            var mechMatl = new Material(mechColor, 0.0f, 0.0f, Guid.NewGuid(), "mech");
            var mechMass = new Mass(new Profile(mechPerim), mechHeight, mechMatl,
                                    new Transform(0.0, 0.0, levels.First().Elevation),
                                    null, Guid.NewGuid(), "");           


            var stairPerim = Polygon.Rectangle(stairLength, stairWidth);
            var stairTopoN = new TopoBox(stairPerim);
            stairPerim = stairPerim.MoveFromTo(stairTopoN.SW, bathTopo.NW);
            stairTopoN = new TopoBox(stairPerim);
            lastLevel = levels.Last();
            var stairHeight = lastLevel.Elevation - levels.First().Elevation + stairEntry;
            var stairMatl = new Material(stairColor, 0.0f, 0.0f, Guid.NewGuid(), "stair");
            var stairMassN = new Mass(new Profile(stairPerim), stairHeight, stairMatl,
                                      new Transform(0.0, 0.0, levels.First().Elevation),
                                      null, Guid.NewGuid(), "");

            stairPerim = Polygon.Rectangle(stairLength, stairWidth);
            var stairTopoS = new TopoBox(stairPerim);
            stairPerim = stairPerim.MoveFromTo(stairTopoS.NW, bathTopo.SW);
            stairTopoS = new TopoBox(stairPerim);
            stairHeight = stairHeight = lastLevel.Elevation - levels.First().Elevation;
            var stairMassS = new Mass(new Profile(stairPerim), stairHeight, stairMatl,
                                      new Transform(0.0, 0.0, levels.First().Elevation),
                                      null, Guid.NewGuid(), "");


            var liftPerim = Polygon.Rectangle(liftSize, liftSize);
            var liftTopo = new TopoBox(liftPerim);
            liftPerim = liftPerim.MoveFromTo(liftTopo.NW, mechTopo.NE);
            liftTopo = new TopoBox(liftPerim);
            var liftHeight = lastLevel.Elevation - levels.First().Elevation;
            var liftMatl = new Material(liftColor, 0.0f, 0.0f, Guid.NewGuid(), "lift");
            var liftMass = new Mass(new Profile(liftPerim), liftHeight, liftMatl,
                                    new Transform(0.0, 0.0, levels.First().Elevation),
                                    null, Guid.NewGuid(), "");
            output.model.AddElement(liftMass);

            var liftSvcFactor = 1;
            for (int i = 0; i < liftBank; i++)
            {
                liftPerim = Polygon.Rectangle(liftSize, liftSize);
                var liftTopo2 = new TopoBox(liftPerim);
                liftPerim = liftPerim.MoveFromTo(liftTopo2.SW, liftTopo.SE);
                liftTopo = new TopoBox(liftPerim);
                lastLevel = levels.SkipLast((int)liftSvc * liftSvcFactor).Last();
                liftSvcFactor++;
                liftHeight = lastLevel.Elevation - levels.First().Elevation;
                liftMass = new Mass(new Profile(liftPerim), liftHeight, liftMatl,
                                    new Transform(0.0, 0.0, levels.First().Elevation),
                                    null, Guid.NewGuid(), "");
                output.model.AddElement(liftMass);
            }

            liftPerim = Polygon.Rectangle(liftSize, liftSize);
            liftTopo = new TopoBox(liftPerim);
            liftPerim = liftPerim.MoveFromTo(liftTopo.SW, mechTopo.SE);
            liftTopo = new TopoBox(liftPerim);
            lastLevel = levels.SkipLast((int)liftSvc * liftSvcFactor).Last();
            liftSvcFactor++;
            liftHeight = lastLevel.Elevation - levels.First().Elevation;
            liftMass = new Mass(new Profile(liftPerim), liftHeight, liftMatl,
                                new Transform(0.0, 0.0, levels.First().Elevation),
                                null, Guid.NewGuid(), "");
            output.model.AddElement(liftMass);
            
            for (int i = 0; i < liftBank; i++)
            {
                liftPerim = Polygon.Rectangle(liftSize, liftSize);
                var liftTopo2 = new TopoBox(liftPerim);
                liftPerim = liftPerim.MoveFromTo(liftTopo2.SW, liftTopo.SE);
                liftTopo = new TopoBox(liftPerim);
                lastLevel = levels.SkipLast((int)liftSvc * liftSvcFactor).Last();
                liftSvcFactor++;
                liftHeight = lastLevel.Elevation - levels.First().Elevation;
                liftMass = new Mass(new Profile(liftPerim), liftHeight, liftMatl,
                                    new Transform(0.0, 0.0, levels.First().Elevation),
                                    null, Guid.NewGuid(), "");
                if (liftMass.Height >= 10.0)
                {
                    output.model.AddElement(liftMass);
                }
            }


            output.model.AddElement(bathMass);
            output.model.AddElement(mechMass);
            output.model.AddElement(stairMassN);
            output.model.AddElement(stairMassS);

            //var output = new CoreByLevelsOutputs(mechMass.Volume());



            // Included for testing only. Remove for production.
            var matl = new Material(new Color(0.5f, 0.5f, 0.5f, 0.5f), 0.0f, 0.0f, Guid.NewGuid(), "Level");
            foreach (var item in levels)
            {
                output.model.AddElement(new Panel(item.Perimeter, matl, new Transform(0.0, 0.0, item.Elevation), null, Guid.NewGuid(), ""));
            }
            // Included for testing only. Remove for production.

            return output;
        }
    }
}