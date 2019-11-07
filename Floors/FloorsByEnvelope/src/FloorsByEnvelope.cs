using Elements;
using Elements.Geometry;
using System.Collections.Generic;

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
            var elementCount = 0.0;
            foreach (var model in inputModels.Values)
            {
                envelopes.AddRange(model.AllElementsOfType<Envelope>());
                elementCount += model.Elements.Count;
            }
            return new FloorsByEnvelopeOutputs(0.0, envelopes.Count, elementCount);
        }
      }
}