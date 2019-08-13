using System;
using System.Collections.Generic;
using System.Linq;
using Elements;
using Elements.Geometry;
using GeometryEx;

namespace Structure
{
  	public static class Structure
	{
		/// <summary>
		/// The Structure function.
		/// </summary>
		/// <param name="model">The model. 
		/// Add elements to the model to have them persisted.</param>
		/// <param name="input">The arguments to the execution.</param>
		/// <returns>A StructureOutputs instance containing computed results.</returns>
		public static StructureOutputs Execute(Model model, StructureInputs input)
		{
            var envelope = model.ElementsOfType<Mass>().ToList().FindAll(m => m.Name == "envelope");
            var core = model.ElementsOfType<Mass>().ToList().Find(m => m.Name == "core");
            if (envelope.Count == 0)
            {
                envelope = new List<Mass>
                {
                    new Mass(Polygon.Rectangle(50.0, 50.0),
                             75.0,
                             new Material("envelope", Palette.Aqua, 0.0f, 0.0f))
                    {
                        Name = "envelope"
                    }
                };
                model.AddElement(envelope.First());
            }
            var columns = 0;
            foreach (var mass in envelope)
            {
                var height = mass.Height;
                var perimeter = mass.Profile.Perimeter;
                var grid = new CoordGrid(perimeter, input.ColumnXAxisInterval, input.ColumnYAxisInterval);
                if (core != null)
                {
                    grid.Allocate(core.Profile.Perimeter);
                }
                var points = grid.Available;
                foreach (var pnt in points)
                {
                    var t = new Transform(mass.Transform);
                    var polygon = Polygon.Rectangle(input.ColumnDiameter, input.ColumnDiameter).MoveFromTo(Vector3.Origin, pnt);
                    if (!perimeter.Covers(polygon))
                    {
                        continue;
                    }
                    model.AddElement(new Mass(new Profile(polygon), height, BuiltInMaterials.Concrete, t));
                    columns++;
                }
            }
			var output = new StructureOutputs(columns);
			return output;
		}
  	}
}