using System.Collections.Generic;
using System.Linq;
using System;
using Elements;
using Elements.Geometry;
using GeometryEx;

namespace EnvelopeBySketch
{
      public static class EnvelopeBySketch
    {
        /// <summary>
        /// The EnvelopeBySketch function.
        /// </summary>
        /// <param name="model">The input model.</param>
        /// <param name="input">The arguments to the execution.</param>
        /// <returns>A EnvelopeBySketchOutputs instance containing computed results and the model with any new elements.</returns>
        public static EnvelopeBySketchOutputs Execute(Dictionary<string, Model> inputModels, EnvelopeBySketchInputs input)
        {
            //var footprint = new Envelope(new Transform(), new Material("basement", Palette.Gray, 0.0f, 0.0f), new Representation(new List<Elements.Geometry.Solids.SolidOperation> { new Elements.Geometry.Solids.Extrude(input.Perimeter, input.BuildingHeight, Vector3.ZAxis, 0.0, false) }), Guid.NewGuid(), "");

            var footprint = new Mass(new Profile(input.Perimeter), input.FoundationDepth);
            if (footprint == null)
            {
                footprint = new Elements.Mass(Polygon.Rectangle(50.0, 50.0), 0.1, BuiltInMaterials.Concrete);
            }
            var basement = new Elements.Mass(input.Perimeter,
                                             input.FoundationDepth,
                                             new Material("basement", Palette.Gray, 0.0f, 0.0f),
                                             new Transform(footprint.Transform.Matrix));
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
                                          new Transform(footprint.Transform.Matrix));
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
                                              new Transform(footprint.Transform.Matrix));
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
            var output = new EnvelopeBySketchOutputs(bldgHeight,
                                                     input.FoundationDepth,
                                                     input.SetbackInterval,
                                                     input.SetbackDepth,
                                                     volume);
            foreach (var mass in masses)
            {
                output.model.AddElement(mass);
            }
            return output;
        }
      }
}