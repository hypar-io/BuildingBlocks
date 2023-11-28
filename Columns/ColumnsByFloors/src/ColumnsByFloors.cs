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
            var output = new ColumnsByFloorsOutputs();
            var allFloors = new List<Floor>();
            inputModels.TryGetValue("Floors", out var flrModel);
            if (flrModel == null)
            {
                output.Errors.Add("The model output named 'Floors' could not be found. Check the upstream functions for errors.");
                return output;
            }
            else if (flrModel.AllElementsOfType<Floor>().Count() == 0)
            {
                output.Errors.Add($"No Floors found in the model 'Floors'. Check the output from the function upstream that has a model output 'Floors'.");
                return output;
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

                        columns.Add(new Column(point, height, new Line(point, point + Vector3.ZAxis * height),
                         Polygon.Rectangle(input.ColumnDiameter, input.ColumnDiameter), 0, 0, input.GridRotation, new Transform(0, 0, floor.Elevation + floor.Thickness),
                          BuiltInMaterials.Concrete, null, false, Guid.NewGuid(), "ColumnsByFloors"));
                    }
                }
            }

            output.ColumnQuantity = columns.Count;
            output.Model.AddElements(columns);
            return output;
        }
    }
}