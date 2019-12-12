using Elements;
using Elements.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using GeometryEx;

namespace LevelsByEnvelope
{
    public static class LevelsByEnvelope
    {
        /// <summary>
        /// The LevelsByEnvelope function.
        /// </summary>
        /// <param name="model">The input model.</param>
        /// <param name="input">The arguments to the execution.</param>
        /// <returns>A LevelsByEnvelopeOutputs instance containing computed results and the model with any new elements.</returns>
        public static LevelsByEnvelopeOutputs Execute(Dictionary<string, Model> inputModels, LevelsByEnvelopeInputs input)
        {
            var envelopes = new List<Envelope>();
            inputModels.TryGetValue("Envelope", out var model);
            if (model == null)
            {
                throw new ArgumentException("No Envelope found.");
            }
            envelopes.AddRange(model.AllElementsOfType<Envelope>());
            var mechHeight = input.StandardLevelHeight * input.MechanicalLevelHeightRatio;
            var levelMaker = new LevelMaker(envelopes, input.StandardLevelHeight, input.GroundLevelHeight, mechHeight);
            var levelArea = 0.0;
            foreach (var level in levelMaker.Levels)
            {
                levelArea += level.Perimeter.Area();
            }
            var output = new LevelsByEnvelopeOutputs(levelMaker.Levels.Count(), levelArea, input.GroundLevelHeight, mechHeight);
            output.model.AddElements(levelMaker.Levels);     
            return output;
        }
    }
}