using System;
using System.Collections.Generic;
using System.Linq;
using Elements;
using Elements.Geometry;
using GeometryEx;

namespace Facade
{
  	public static class Facade
	{
        /// <summary>
        /// Adds facade Panels to one or more Masses named 'envelope'.
        /// </summary>
        /// <param name="model">The model. 
        /// Add elements to the model to have them persisted.</param>
        /// <param name="input">The arguments to the execution.</param>
        /// <returns>A FacadeOutputs instance containing computed results.</returns>
        public static FacadeOutputs Execute(Dictionary<string, Model> models, FacadeInputs input)
		{
            var envelope = models.ContainsKey("envelope") ? models["envelope"].ElementsOfType<Mass>().ToList().FindAll(m => m.Name == "envelope") : null;
            if (envelope == null || envelope.Count == 0)
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
            }
            var panels = 0;
            var matl = new Material("envelope", Palette.Aqua, 0.0f, 0.0f);
            var outputModel = new Model();
            foreach (var mass in envelope)
            {
                var perimeters = mass.Profile.Perimeter.Offset(-0.2);
                if (perimeters.Count() == 0 || mass.Transform.Origin.Z < -0.5)
                {
                    continue;
                }
                var profile = new Profile(perimeters.First());
                var facade = new Mass(profile, mass.Height, BuiltInMaterials.Concrete, new Transform(mass.Transform));
                foreach (var bot in facade.ProfileTransformed().Perimeter.Segments())
                {
                    var top = new Line(new Vector3(bot.Start.X, bot.Start.Y, bot.Start.Z + mass.Height),
                                       new Vector3(bot.End.X, bot.End.Y, bot.End.Z + mass.Height));
                    var grid = new Grid(bot, 
                                        top, 
                                       (int)input.HorizontalPanelsPerFacade, 
                                       (int)input.VerticalPanelsPerFacade);
                    foreach (var cell in grid.Cells())
                    {
                        var panel = new Panel(new Polygon(cell.Shrink(input.MullionWidth)), matl);
                        outputModel.AddElement(panel);
                        panels++;
                    }
                }
            }
            var output = new FacadeOutputs(panels);
            output.model = outputModel;
			return output;
		}
  	}
}