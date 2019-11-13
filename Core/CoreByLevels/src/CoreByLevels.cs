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
            const double freightService = 46000.0;
            const double occupantLoad = 9.3;
            const double liftSize = 3.0;
            const double stairLength = 8.0;
            const double stairWidth = 4.0;
            const double stairOver = 2.0;
            const double stairEntry = 3.0;
            const double bathLength = 4.0;
            const double bathWidth = 12.0;
            const double mechLength = 2.0;
            const double mechWidth = 20.0;

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
            var occupants = Math.Ceiling(occArea / occupantLoad);
            var liftQty = Math.Ceiling(occArea / liftService);
            var liftBank = Math.Ceiling(liftQty / 2);
            //var frtQty = Math.Floor(occArea / freightService);

            var coreLength = mechLength + bathLength + stairOver + (liftBank * liftSize);
            var coreWidth = (stairWidth * 2) + bathWidth;
            var corePerim = Polygon.Rectangle(coreLength, coreWidth);
            var coreTopo = new TopoBox(corePerim);
            var coreBase = levels.First().Elevation;
            var coreHeight = Math.Abs(coreBase) + levels.Last().Elevation + stairEntry;

            var bathPerim = Polygon.Rectangle(bathLength, bathWidth);
            var bathTopo = new TopoBox(bathPerim);
            bathPerim = bathPerim.MoveFromTo(bathTopo.W, coreTopo.W);

            var mechPerim = Polygon.Rectangle(mechLength, mechWidth);
            var mechTopo = new TopoBox(mechPerim);
            mechPerim = mechPerim.MoveFromTo(mechTopo.W, bathTopo.E);

            

            var position = levels.First().Perimeter.Centroid();
            var mass = new Mass(new Profile(corePerim), coreHeight, BuiltInMaterials.Concrete, 
                                new Transform(position.X, position.Y, coreBase), 
                                null, Guid.NewGuid(), "");
            var output = new CoreByLevelsOutputs(mass.Volume());
            output.model.AddElement(mass);
            return output;
        }
    }
}