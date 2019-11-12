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
            // Create the foundation.
            var extrude = new Elements.Geometry.Solids.Extrude(input.Perimeter, input.FoundationDepth, Vector3.ZAxis, 0.0, false);
            var geomRep = new Representation(new List<Elements.Geometry.Solids.SolidOperation>() { extrude });
            var fndMatl = new Material("foundation", Palette.Gray, 0.0f, 0.0f);
            var envMatl = new Material("envelope", Palette.Aqua, 0.0f, 0.0f);
            var envelope = new List<Envelope>()
            {
                new Envelope(input.Perimeter, input.FoundationDepth * -1, input.FoundationDepth, Vector3.ZAxis,
                             0.0, new Transform(0.0, 0.0, input.FoundationDepth * -1), fndMatl, geomRep, Guid.NewGuid(), "")
            };

            // Create the envelope at the location's zero plane.
            var bldgHeight = input.BuildingHeight <= input.SetbackInterval ? input.BuildingHeight : input.SetbackInterval;

            extrude = new Elements.Geometry.Solids.Extrude(input.Perimeter, bldgHeight, Vector3.ZAxis, 0.0, false);
            geomRep = new Representation(new List<Elements.Geometry.Solids.SolidOperation>() { extrude });
            envelope.Add(new Envelope(input.Perimeter, 0.0, bldgHeight, Vector3.ZAxis, 0.0,
                                      new Transform(), envMatl, geomRep, Guid.NewGuid(), ""));

            // Create the remaining envelope elements.
            var offsetFactor = -1;
            while (bldgHeight < input.BuildingHeight)
            {
                var height = input.BuildingHeight - bldgHeight < input.SetbackInterval ? input.BuildingHeight - bldgHeight : input.SetbackInterval;
                var tryPer = input.Perimeter.Offset(input.SetbackDepth * offsetFactor);
                if (tryPer.Count() > 0)
                {
                    var profile = new Profile(tryPer.OrderByDescending(p => p.Area()).First());
                    if (profile.Area() < 100.0)
                    {
                        break;
                    }
                    extrude = new Elements.Geometry.Solids.Extrude(profile, height, Vector3.ZAxis, 0.0, false);
                    geomRep = new Representation(new List<Elements.Geometry.Solids.SolidOperation>() { extrude });
                    envelope.Add(new Envelope(profile, bldgHeight, bldgHeight, Vector3.ZAxis, 0.0,
                                 new Transform(0.0, 0.0, bldgHeight), envMatl, geomRep, Guid.NewGuid(), ""));
                    offsetFactor--;
                    bldgHeight += height;
                }
                else
                {
                    break;
                }
            }

            var output = new EnvelopeBySketchOutputs(input.BuildingHeight, input.FoundationDepth);
            foreach (var item in envelope.OrderBy(e => e.Elevation))
            {
                output.model.AddElement(item);
            }
            return output;
        }
    }
}