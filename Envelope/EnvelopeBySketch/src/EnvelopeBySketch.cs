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
            // Create the foundation Envelope.
            var extrude = new Elements.Geometry.Solids.Extrude(input.Perimeter, input.FoundationDepth, Vector3.ZAxis, 0.0, false);
            var geomRep = new Representation(new List<Elements.Geometry.Solids.SolidOperation>() { extrude });
            var fndMatl = new Material("foundation", Palette.Gray, 0.0f, 0.0f);
            var envMatl = new Material("envelope", Palette.Aqua, 0.0f, 0.0f);
            var envelopes = new List<Envelope>()
            {
                new Envelope(input.Perimeter, input.FoundationDepth * -1, input.FoundationDepth, Vector3.ZAxis,
                             0.0, new Transform(0.0, 0.0, input.FoundationDepth * -1), fndMatl, geomRep, Guid.NewGuid(), "")
            };

            // Create the Envelope at the location's zero plane.
            var tiers = Math.Floor(input.BuildingHeight / input.SetbackInterval);
            var tierHeight = tiers > 1 ? input.BuildingHeight / tiers : input.BuildingHeight;
            extrude = new Elements.Geometry.Solids.Extrude(input.Perimeter, tierHeight, Vector3.ZAxis, 0.0, false);
            geomRep = new Representation(new List<Elements.Geometry.Solids.SolidOperation>() { extrude });
            envelopes.Add(new Envelope(input.Perimeter, 0.0, tierHeight, Vector3.ZAxis, 0.0,
                          new Transform(), envMatl, geomRep, Guid.NewGuid(), ""));
            // Create the remaining Envelope Elements.
            var offsFactor = -1;
            var elevFactor = 1;
            for (int i = 0; i < tiers - 1; i++)
            {
                var tryPer = input.Perimeter.Offset(input.SetbackDepth * offsFactor);
                if (tryPer.Count() == 0 || tryPer.First().Area() < input.MinimumTierArea)
                {
                    break;
                }
                tryPer = tryPer.OrderByDescending(p => p.Area()).ToArray();
                extrude = new Elements.Geometry.Solids.Extrude(tryPer.First(), tierHeight, Vector3.ZAxis, 0.0, false);
                geomRep = new Representation(new List<Elements.Geometry.Solids.SolidOperation>() { extrude });
                envelopes.Add(new Envelope(tryPer.First(), tierHeight * elevFactor, tierHeight, Vector3.ZAxis, 0.0,
                            new Transform(0.0, 0.0, tierHeight * elevFactor), envMatl, geomRep, Guid.NewGuid(), ""));
                offsFactor--;
                elevFactor++;
            }
            var output = new EnvelopeBySketchOutputs(input.BuildingHeight, input.FoundationDepth);
            envelopes = envelopes.OrderBy(e => e.Elevation).ToList();
            foreach (var env in envelopes)
            {
                output.model.AddElement(env);
            }
            return output;
        }
    }
}