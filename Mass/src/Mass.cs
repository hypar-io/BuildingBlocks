using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Elements;
using Elements.Geometry;
using GeometryEx;

namespace Mass
{
  	public static class Mass
	{
		/// <summary>
		/// The Mass function.
		/// </summary>
		/// <param name="model">The model. 
		/// Add elements to the model to keep them persistent.</param>
		/// <param name="input">The arguments to the execution.</param>
		/// <returns>A MassOutputs instance containing computed results.</returns>
		public static MassOutputs Execute(Dictionary<string, Model> models, MassInputs input)
		{
            var footprint = models.ContainsKey("site") ? models["site"].ElementsOfType<Elements.Mass>().ToList().Find(m => m.Name == "footprint") : null;
            if (footprint == null)
            {
                footprint = new Elements.Mass(Polygon.Rectangle(50.0, 50.0), 0.1, BuiltInMaterials.Concrete);
            }
            var basement = new Elements.Mass(footprint.Profile,
                                             input.FoundationDepth,
                                             new Material("basement", Palette.Gray, 0.0f, 0.0f),
                                             new Transform(footprint.Transform.Matrix))
            {
                Name = "envelope"
            };
            basement.Transform.Move(new Vector3(0.0, 0.0, (input.FoundationDepth * -1) - footprint.Height));
            var volume = basement.Profile.Perimeter.Area() * basement.Height;
            var masses = new List<Elements.Mass>
            {
                basement
            };
            var bldgHeight = input.BuildingHeight <= input.SetbackInterval ? input.BuildingHeight : input.SetbackInterval;
            // var matl = new Material("envelope", Palette.Aqua, 0.0f, 0.0f);
            var matl = BuiltInMaterials.Glass;
            var story = new Elements.Mass(footprint.Profile, 
                                          bldgHeight, 
                                          matl, 
                                          new Transform(footprint.Transform.Matrix))
            {
                Name = "envelope"
            };
            story.Transform.Move(new Vector3(0.0, 0.0, -0.1));
            masses.Add(story);
            volume += story.Profile.Perimeter.Area() * story.Height;
            while (bldgHeight < input.BuildingHeight)
            {
                var trans = new Transform(story.Transform.Matrix);
                var height = input.BuildingHeight - bldgHeight < input.SetbackInterval ? input.BuildingHeight - bldgHeight : input.SetbackInterval;
                var tryPerim = story.Profile.Perimeter.Offset(input.SetbackDepth * -1.0);
                if (tryPerim.Count() > 0)
                {
                    story = new Elements.Mass(new Profile(tryPerim.OrderByDescending(p => p.Area()).First()),
                                              height,
                                              matl,
                                              new Transform(footprint.Transform.Matrix))
                    {
                        Name = "envelope"
                    };
                    story.Transform.Move(new Vector3(0.0, 0.0, bldgHeight));
                    masses.Add(story);
                    volume += story.Profile.Perimeter.Area() * height;
                    bldgHeight += height;
                }
                else
                {
                    break;
                }
            }
            var output = new MassOutputs(bldgHeight,
                                         input.FoundationDepth,
                                         input.SetbackInterval,
                                         input.SetbackDepth,
                                         volume);
            foreach (var mass in masses)
            {
                output.model.AddElement(mass);
            }
            output.model.AddElement(new Polygon(new Vector3[] { new Vector3(0.0, 0.0), });
            return output;
        }
    }
}