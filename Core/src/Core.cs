using System.Collections.Generic;
using System.Linq;
using Elements;
using Elements.Geometry;
using GeometryEx;

namespace Core
{
  	public static class Core
	{
		/// <summary>
		/// The Core function.
		/// </summary>
		/// <param name="model">The model. 
		/// Add elements to the model to have them persisted.</param>
		/// <param name="input">The arguments to the execution.</param>
		/// <returns>A CoreOutputs instance containing computed results.</returns>
		public static CoreOutputs Execute(Model model, CoreInputs input)
		{
            var envelope = model.ElementsOfType<Mass>().ToList().FindAll(m => m.Name == "envelope").OrderBy(m => m.Transform.Origin.Z).ToList();
            if (envelope.Count == 0)
            {
                envelope = new List<Mass>
                {
                    new Mass(Polygon.Rectangle(50.0, 50.0),
                             75.0,
                             new Material("envelope", Palette.Aqua, 0.0f, 0.0f))
                };
                model.AddElement(envelope.First());
            }
            var height = 0.0;
            foreach (var item in envelope)
            {
                height += item.Height;
            }
            var mass = new Mass(Polygon.Rectangle(input.Length, input.Width),
                                height,
                                new Material("core", Palette.Stone, 0.0f, 0.0f),
                                new Transform(envelope.First().Transform.Matrix))
            {
                Name = "core"
            };
            model.AddElement(mass);
            var output = new CoreOutputs(input.Length * input.Width, height);
			return output;
		}
    }
}