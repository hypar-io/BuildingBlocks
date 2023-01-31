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
        /// Creates a grid of columns derived from x- and y-axis intervals and a rotation angle using the largest floor to determine the pattern for all floors.
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
                var biggestFloor = floorGroup.OrderByDescending(f => f.Profile.Perimeter.Area()).First();
                var grid = new CoordinateGrid(biggestFloor.Profile.Perimeter, input.GridXAxisInterval, input.GridYAxisInterval, input.GridRotation);

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

                    foreach (var point in grid.Available)
                    {
                        var colPerim = Polygon.Rectangle(input.ColumnDiameter, input.ColumnDiameter).MoveFromTo(Vector3.Origin, point);
                        if (!floor.Profile.Perimeter.Covers(colPerim) ||
                            !ceiling.Profile.Perimeter.Covers(colPerim))
                        {
                            continue;
                        }
                        columns.Add(new Column()
                        {
                            Location = point,
                            Height = height,
                            Profile = Polygon.Rectangle(input.ColumnDiameter, input.ColumnDiameter),
                            Material = BuiltInMaterials.Concrete,
                            Transform = new Transform(0, 0, floor.Elevation + floor.Thickness),
                            Rotation = input.GridRotation
                        });
                    }
                }
            }
            var output = new ColumnsByFloorsOutputs(columns.Count());
            columns.ForEach(c => output.Model.AddElement(c));
            return output;
        }
    }
}