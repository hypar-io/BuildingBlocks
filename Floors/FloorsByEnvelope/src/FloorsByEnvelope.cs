using Elements;
using Elements.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FloorsByEnvelope
{
      public static class FloorsByEnvelope
    {
        /// <summary>
        /// The FloorsByEnvelope function.
        /// </summary>
        /// <param name="model">The input model.</param>
        /// <param name="input">The arguments to the execution.</param>
        /// <returns>A FloorsByEnvelopeOutputs instance containing computed results and the model with any new elements.</returns>
        public static FloorsByEnvelopeOutputs Execute(Dictionary<string, Model> inputModels, FloorsByEnvelopeInputs input)
        {
            var envelopes = new List<Envelope>();
            inputModels.TryGetValue("Envelope", out var model);
            envelopes.AddRange(model.AllElementsOfType<Envelope>());
            var bldgHeight = 0.0;
            var rentable = new List<Envelope>();
            var subgrade = new List<Envelope>();
            var floors = new List<Floor>();
            foreach (var envelope in envelopes)
            {
                if (envelope.Direction == Vector3.ZAxis)
                {
                    rentable.Add(envelope);
                    bldgHeight += envelope.Height;
                }
                else
                {
                    subgrade.Add(envelope);
                }
            }
            rentable.OrderBy(e => e.Elevation);
            var perimeter = rentable.First().Profile.Perimeter;
            var mechFloorHeight = input.FloorToFloorHeight * input.MechanicalFloorHeightRatio;
            var mechFloorElevation = 0.0;
            if (bldgHeight >= mechFloorHeight)
            {
                mechFloorElevation = bldgHeight - mechFloorHeight;
            }
            floors.Add(new Floor(perimeter, 0.15, mechFloorElevation - 0.15, 
                                 new Transform(), BuiltInMaterials.Concrete, 
                                 null, Guid.NewGuid(), ""));
            return new FloorsByEnvelopeOutputs(0.0, envelopes.Count);
        }
      }
}