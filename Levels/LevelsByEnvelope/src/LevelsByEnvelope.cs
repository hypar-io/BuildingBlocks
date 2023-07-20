using Elements;
using Elements.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LevelsByEnvelope
{
    public static class LevelsByEnvelope
    {
        /// <summary>
        /// makes levels by envelope.
        /// </summary>
        /// <param name="model">The input model.</param>
        /// <param name="input">The arguments to the execution.</param>
        /// <returns>A LevelsByEnvelopeOutputs instance containing computed results and the model with any new elements.</returns>
        public static LevelsByEnvelopeOutputs Execute(Dictionary<string, Model> inputModels, LevelsByEnvelopeInputs input)
        {
            var envelopes = new List<Envelope>();
            inputModels.TryGetValue("Envelope", out var model);
            var output = new LevelsByEnvelopeOutputs();
            if (model == null)
            {
                output.Errors.Add("The model output named 'Envelope' could not be found. Check the upstream functions for errors.");
                return output;
            }
            else if (model.AllElementsOfType<Envelope>().Count() == 0)
            {
                output.Errors.Add($"No Envelopes found in the model 'Envelope'. Check the output from the function upstream that has a model output 'Envelope'.");
                return output;
            }
            envelopes.AddRange(model.AllElementsOfType<Envelope>());
            var levelMaker = new LevelMaker(envelopes,
                                            input.StandardLevelHeight,
                                            input.GroundLevelHeight,
                                            input.PenthouseLevelHeight);
            var levelArea = 0.0;
            foreach (var lp in levelMaker.LevelPerimeters)
            {
                levelArea += lp.Area;
            }

            output.LevelQuantity = levelMaker.Levels.Count();
            output.TotalLevelArea = levelArea;
            output.EntryLevelHeight = input.GroundLevelHeight;
            output.RepeatingLevelHeight = input.StandardLevelHeight;
            output.TopLevelHeight = input.PenthouseLevelHeight;

            output.Model.AddElement(levelMaker.LevelGroup);
            output.Model.AddElements(levelMaker.Levels);
            output.Model.AddElements(levelMaker.LevelPerimeters);
            output.Model.AddElements(levelMaker.LevelVolumes);
            output.Model.AddElements(levelMaker.ViewScopes);
            var matl = BuiltInMaterials.Glass;
            matl.SpecularFactor = 0.0;
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