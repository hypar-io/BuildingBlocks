using Elements;
using Elements.Geometry;
using System;
using System.Linq;
using System.Collections.Generic;
using GeometryEx;

namespace ColumnsByFloors
{
    public static class ColumnsByFloors
    {
        /// <summary>
        /// The ColumnsByFloors function.
        /// </summary>
        /// <param name="model">The input model.</param>
        /// <param name="input">The arguments to the execution.</param>
        /// <returns>A ColumnsByFloorsOutputs instance containing computed results and the model with any new elements.</returns>
        public static ColumnsByFloorsOutputs Execute(Dictionary<string, Model> inputModels, ColumnsByFloorsInputs input)
        {
            var allFloors = new List<Floor>();
            inputModels.TryGetValue("Floors", out var flrModel);
            if (flrModel == null || flrModel.AllElementsOfType<Floor>().Count() == 0)
            {
                throw new ArgumentException("No Floors found.");
            }
            allFloors.AddRange(flrModel.AllElementsOfType<Floor>());
            var floorGroups = new List<List<Floor>>();
            while (allFloors.Count > 0)
            {
                var testPerimeter = allFloors.First().Profile.Perimeter;
                var floors = new List<Floor>();
                foreach (var floor in allFloors.ToList())
                {
                    if (testPerimeter.Intersects(floor.Profile.Perimeter))
                    {
                        floors.Add(floor);
                        allFloors.Remove(floor);
                    }
                }
                floorGroups.Add(floors);
            }
            var columns = new List<Column>();
            foreach (var floorGroup in floorGroups)
            {
                var floors = floorGroup.OrderBy(f => f.Elevation).ToList();
                for (var i = 0; i < floors.Count() - 1; i++)
                {
                    var floor = floors.ElementAt(i);
                    int next = 1;
                    Floor ceiling = null;
                    do
                    {
                        ceiling = floors.ElementAt(i + next);
                        next++;
                    } while (!floor.Profile.Perimeter.Intersects(ceiling.Profile.Perimeter));
                    
                    var height = ceiling.Elevation - floor.Elevation - floor.Thickness;
                    var grid = new CoordinateGrid(ceiling.Profile.Perimeter, input.GridXAxisInterval, input.GridYAxisInterval, input.GridRotation);
                    foreach (var point in grid.Available)
                    {
                        columns.Add(new Column(point, height,
                                               new Profile(Polygon.Rectangle(input.ColumnDiameter, input.ColumnDiameter)),
                                               BuiltInMaterials.Concrete, 
                                               new Transform(0.0, 0.0, floor.Elevation + floor.Thickness),
                                               0.0, 0.0, input.GridRotation, 
                                               Guid.NewGuid(), ""));
                    }
                }
            }
            var output = new ColumnsByFloorsOutputs(columns.Count());
            foreach (var column in columns)
            {
                output.model.AddElement(column);
            }
            return output;
        }
    }
}