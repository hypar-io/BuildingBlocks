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
        /// Creates Levels and LevelPerimeters from an incoming Envelope and height arguments.
        /// </summary>
        /// <param name="model">The input model.</param>
        /// <param name="input">The arguments to the execution.</param>
        /// <returns>A LevelsByEnvelopeOutputs instance containing computed results and the model with any new elements.</returns>
        public static LevelsByEnvelopeOutputs Execute(Dictionary<string, Model> inputModels, LevelsByEnvelopeInputs input)
        {
            var envelopes = new List<Envelope>();
            inputModels.TryGetValue("Envelope", out var model);
            if (model == null || model.AllElementsOfType<Envelope>().Count() == 0)
            {
                throw new ArgumentException("No Envelope found.");
            }
            envelopes.AddRange(model.AllElementsOfType<Envelope>());
            var levelMaker = new LevelMaker(envelopes, 
                                            input.StandardLevelHeight, 
                                            input.GroundLevelHeight,
                                            input.PenthouseLevelHeight);
            var levelArea = 0.0;
            foreach (var lp in levelMaker.LevelPerimeters)
            {
                levelArea += Math.Abs(lp.Perimeter.Area());
            }
            var output = new LevelsByEnvelopeOutputs(levelMaker.Levels.Count(), 
                                                     levelArea, 
                                                     input.GroundLevelHeight, 
                                                     input.StandardLevelHeight,
                                                     input.PenthouseLevelHeight);
            output.Model.AddElements(levelMaker.Levels);
            output.Model.AddElements(levelMaker.LevelPerimeters);
            var matl = BuiltInMaterials.Glass;
            matl.SpecularFactor = 0.5;
            matl.GlossinessFactor = 0.0;
            foreach (var item in levelMaker.LevelPerimeters)
            {
                output.Model.AddElement(new Panel(item.Perimeter, matl, new Transform(0.0, 0.0, item.Elevation), 
                                        null, false, Guid.NewGuid(), ""));
            }
            return output;
        }
    }
}