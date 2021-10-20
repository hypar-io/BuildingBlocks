using Elements;
using Elements.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ColumnsFromGrid
{
    public static class ColumnsFromGrid
    {
        /// <summary>
        /// The ColumnsFromGrid function.
        /// </summary>
        /// <param name="model">The input model.</param>
        /// <param name="input">The arguments to the execution.</param>
        /// <returns>A ColumnsFromGridOutputs instance containing computed results and the model with any new elements.</returns>
        public static ColumnsFromGridOutputs Execute(Dictionary<string, Model> inputModels, ColumnsFromGridInputs input)
        {
            if (!inputModels.TryGetValue("Grids", out var gridsModel))
            {
                throw new ArgumentException("No Grids found.");
            }

            var envelopePolygons = GetEnvelopePolygons(inputModels);
            var grids = gridsModel.AllElementsOfType<Grid2dElement>();

            var columns = new List<Column>();
            foreach (var grid in grids)
            {
                var alignedTransform = grid.Transform.Moved(-1 * grid.Transform.Origin);
                foreach (var gridNode in grid.GridNodes)
                {
                    if (!envelopePolygons.Any() || envelopePolygons.Any(p => p.Contains(gridNode.Location.Origin)))
                    {
                        columns.Add(CreateColumn(input, alignedTransform, gridNode));
                    }
                }
            }

            var output = new ColumnsFromGridOutputs();
            output.Model.AddElements(columns);

            return output;
        }

        private static Column CreateColumn(ColumnsFromGridInputs input, Transform alignedTransform, GridNode gridNode)
        {
            var locationOverride = input.Overrides?.ColumnPositions?.FirstOrDefault(p => p.Identity.Name.Equals(gridNode.Name));
            var sizeOverrideValue = input.Overrides?.Sizes?.FirstOrDefault(p => p.Identity.Name.Equals(gridNode.Name))?.Value
                                    ?? new SizesValue(input.Height, input.Width, input.Depth);

            var currentAlignedPerim = Polygon.Rectangle(sizeOverrideValue.Width, sizeOverrideValue.Depth)
                                             .TransformedPolygon(alignedTransform);

            var column = new Column(locationOverride?.Value.Location ?? gridNode.Location.Origin,
                            sizeOverrideValue.Height,
                            currentAlignedPerim,
                            BuiltInMaterials.Steel,
                            id: Guid.NewGuid(),
                            name: gridNode.Name);

            if (locationOverride != null)
            {
                column.AddOverrideIdentity(ColumnPositionsOverride.Name, locationOverride.Id, locationOverride.Identity);
            }

            column.AdditionalProperties["Width"] = sizeOverrideValue.Width;
            column.AdditionalProperties["Depth"] = sizeOverrideValue.Depth;
            column.AdditionalProperties["Height"] = sizeOverrideValue.Height;

            return column;
        }

        private static List<Polygon> GetEnvelopePolygons(Dictionary<string, Model> inputModels)
        {
            var envelopePolygons = new List<Polygon>();
            if (inputModels.TryGetValue("Envelope", out var envelopeModel))
            {
                var envelopes = envelopeModel.AllElementsOfType<Envelope>();
                // Handle all envelopes which are extrusions. This is the old way.
                if (envelopes.All(e => e.Profile != null))
                {
                    envelopePolygons = envelopes.Select(e => (Polygon)e.Profile.Perimeter.Transformed(e.Transform)).ToList();
                }
                // Handle envelopes of all shapes by using a more general method of convex hull projection onto the XY plane.
                else
                {
                    foreach (var e in envelopes)
                    {
                        var polygon = ConvexHull.FromPoints(e.Representation.SolidOperations.SelectMany(o => o.Solid.Vertices.Select(v => new Vector3(v.Value.Point.X, v.Value.Point.Y))));
                        envelopePolygons.Add(polygon);
                    }
                }
            }

            return envelopePolygons;
        }
    }
}