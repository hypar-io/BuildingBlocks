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
		public static CoreOutputs Execute(Dictionary<string, Model> models, CoreInputs input)
		{
            var envelope = models.ContainsKey("envelope") ?
                models["envelope"].ElementsOfType<Mass>().ToList().FindAll(m => m.Name == "envelope").OrderBy(m => m.Transform.Origin.Z).ToList()
                : null;
            if (envelope == null || envelope.Count == 0)
            {
                envelope = new List<Mass>
                {
                    new Mass(Polygon.Rectangle(50.0, 50.0),
                             75.0,
                             new Material("envelope", Palette.Aqua, 0.0f, 0.0f))
                };
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
            var output = new CoreOutputs(input.Length * input.Width, height);
            output.model.AddElement(mass);
			return output;
		}
    }
}