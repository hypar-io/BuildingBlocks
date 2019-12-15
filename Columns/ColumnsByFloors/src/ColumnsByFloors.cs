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
            var floors = new List<Floor>();
            inputModels.TryGetValue("Floors", out var model);
            if (model == null)
            {
                throw new ArgumentException("No Floors found.");
            }
            floors.AddRange(model.AllElementsOfType<Floor>());
            floors = floors.OrderBy(f => f.Elevation).ToList();
            var columns = new List<Column>();
            for (var i = 0; i < floors.Count() - 1; i++)
            {
                var floor = floors.ElementAt(i);
                var ceiling = floors.ElementAt(i + 1);
                var height = ceiling.ProfileTransformed().Perimeter.Vertices.First().Z
                             - floor.ProfileTransformed().Perimeter.Vertices.First().Z
                             - floor.Thickness;
                var grid = new CoordinateGrid(ceiling.Profile.Perimeter, input.GridXAxisInterval, input.GridYAxisInterval, input.GridRotation);
                foreach (var point in grid.Available)
                {
                    columns.Add(new Column(point, height,
                                           new Profile(Polygon.Rectangle(input.ColumnDiameter, input.ColumnDiameter)),
                                           BuiltInMaterials.Concrete, new Transform(0.0, 0.0, floor.Elevation + floor.Thickness),
                                           0.0, 0.0, input.GridRotation, Guid.NewGuid(), ""));
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