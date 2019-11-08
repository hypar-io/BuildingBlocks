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
            
            var rentable = new List<Envelope>();
            var subgrade = new List<Envelope>();
            var floors = new List<Floor>();

            var bldgHeight = 0.0;
            var remnHeight = 0.0;
            foreach (var envelope in envelopes)
            {
                if (envelope.Direction.Z > 0)
                {
                    rentable.Add(envelope);
                    bldgHeight += envelope.Height;
                    remnHeight += envelope.Height;
                }
                else
                {
                    subgrade.Add(envelope);
                }
            }
            rentable = rentable.OrderBy(e => e.Elevation).ToList();
            var perimeter = rentable.First().Profile.Perimeter;
            var offsets = perimeter.Offset(input.FloorSetback * -1);
            if (offsets.Count() > 0)
            {
                perimeter = offsets.First();
            }
            var flrElev = 0.0;
            var floorArea = 0.0;
            var flrThick = 0.15;

            // Add ground floor.
            floors.Add(new Floor(perimeter, flrThick, 0.0, new Transform(0.0, 0.0, flrElev),
                                 BuiltInMaterials.Concrete, null, Guid.NewGuid(), ""));
            if (bldgHeight >= input.GroundFloorHeight)
            {
                flrElev = input.GroundFloorHeight;
                floors.Add(new Floor(perimeter, flrThick, 0.0, new Transform(0.0, 0.0, flrElev),
                           BuiltInMaterials.Concrete, null, Guid.NewGuid(), ""));
                floorArea += perimeter.Area();
                remnHeight = bldgHeight - input.GroundFloorHeight;
            }         

            // Add top mechanical floor.
            var mechHeight = input.StandardFloorHeight * input.MechanicalFloorHeightRatio;
            if (remnHeight >= mechHeight)
            {
                floors.Add(new Floor(perimeter, flrThick, 0.0, new Transform(0.0, 0.0, bldgHeight - mechHeight - flrThick),
                                     BuiltInMaterials.Concrete, null, Guid.NewGuid(), ""));
                remnHeight -= mechHeight;
            }

            // Add higher top floor to accommodate piping under top mechanical floor.
            if (remnHeight > input.StandardFloorHeight + 0.3)
            {
                flrElev = bldgHeight - mechHeight - input.StandardFloorHeight + 0.3;
                floors.Add(new Floor(perimeter, flrThick, 0.0, new Transform(0.0, 0.0, flrElev - flrThick),
                                     BuiltInMaterials.Concrete, null, Guid.NewGuid(), ""));
                remnHeight -= input.StandardFloorHeight + 0.3;
            }

            // Add standard height floors.
            flrElev = input.GroundFloorHeight + input.StandardFloorHeight;
            remnHeight -= input.StandardFloorHeight;
            var flrQty = Math.Floor(remnHeight / input.StandardFloorHeight);
            var stdHeight = remnHeight / flrQty;
            for (int i =0; i < flrQty; i++)
            {
                floors.Add(new Floor(perimeter, flrThick, 0.0, new Transform(0.0, 0.0, flrElev),
                           BuiltInMaterials.Concrete, null, Guid.NewGuid(), ""));
                floorArea += perimeter.Area();
                flrElev += stdHeight;
            }

            var output = new FloorsByEnvelopeOutputs(floorArea, input.GroundFloorHeight, stdHeight, mechHeight, input.FloorSetback);
            output.model.AddElements(floors);
            return output;
        }
    }
}