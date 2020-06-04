using System.Collections.Generic;
using System.Linq;
using System;
using Elements;
using Elements.Geometry;

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
            var extrude = new Elements.Geometry.Solids.Extrude(input.Perimeter, input.FoundationDepth, Vector3.ZAxis, false);
            var geomRep = new Representation(new List<Elements.Geometry.Solids.SolidOperation>() { extrude });
            var envMatl = new Material("envelope", new Color(0.30000001192092896, 0.699999988079071, 0.699999988079071, 0.6), 0.0f, 0.0f);
            var envelopes = new List<Envelope>()
            {
                new Envelope(input.Perimeter, input.FoundationDepth * -1, input.FoundationDepth, Vector3.ZAxis,
                             0.0, new Transform(0.0, 0.0, input.FoundationDepth * -1), envMatl, geomRep, false, Guid.NewGuid(), "")
            };

            // Create the Envelope at the location's zero plane.
            var tiers = Math.Floor(input.BuildingHeight / input.SetbackInterval);
            var tierHeight = tiers > 1 ? input.BuildingHeight / tiers : input.BuildingHeight;
            var polygon = input.Perimeter;
            if (polygon.IsClockWise())
            {
                polygon = polygon.Reversed();
            }
            extrude = new Elements.Geometry.Solids.Extrude(polygon, tierHeight, Vector3.ZAxis, false);
            geomRep = new Representation(new List<Elements.Geometry.Solids.SolidOperation>() { extrude });
            envelopes.Add(new Envelope(input.Perimeter, 0.0, tierHeight, Vector3.ZAxis, 0.0,
                          new Transform(), envMatl, geomRep, false, Guid.NewGuid(), ""));
            // Create the remaining Envelope Elements.
            var offsFactor = -1;
            var elevFactor = 1;
            for (int i = 0; i < tiers - 1; i++)
            {
                var tryPer = input.Perimeter.Offset(input.SetbackDepth * offsFactor);
                tryPer = tryPer.OrderByDescending(p => p.Area()).ToArray();
                if (tryPer.Count() == 0 || tryPer.First().Area() < input.MinimumTierArea)
                {
                    break;
                }
                polygon = tryPer.First();
                if (polygon.IsClockWise())
                {
                    polygon = polygon.Reversed();
                }
                extrude = new Elements.Geometry.Solids.Extrude(polygon, tierHeight, Vector3.ZAxis, false);
                geomRep = new Representation(new List<Elements.Geometry.Solids.SolidOperation>() { extrude });
                envelopes.Add(new Envelope(tryPer.First(), tierHeight * elevFactor, tierHeight, Vector3.ZAxis, 0.0,
                              new Transform(0.0, 0.0, tierHeight * elevFactor), envMatl, geomRep, false, Guid.NewGuid(), ""));
                offsFactor--;
                elevFactor++;
            }
            var output = new EnvelopeBySketchOutputs(input.BuildingHeight, input.FoundationDepth);
            envelopes = envelopes.OrderBy(e => e.Elevation).ToList();
            foreach (var env in envelopes)
            {
                output.Model.AddElement(env);
            }
            return output;
        }
    }
}