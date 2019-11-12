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
            envelopes.AddRange(model.AllElementsOfType<Envelope>());
            var bldgHeight = envelopes.Last().Elevation + envelopes.Last().Height;
            var levels = new List<Level>();
            
            // Add subgrade Levels.
            var subs = envelopes.Where(e => e.Elevation < 0.0).ToList();
            foreach (var env in subs)
            {
                levels.AddRange(LevelMaker.MakeLevels(env, input.StandardLevelHeight, false));
            }

            // Add lobby envelope Levels.
            var envelope = envelopes.Where(e => e.Elevation >= 0.0).ToList().First();
            levels.Add(LevelMaker.MakeLevel(envelope, envelope.Elevation));
            if (envelope.Height >= input.GroundLevelHeight + (input.StandardLevelHeight * 2))
            {
                envelope = new Envelope(envelope.Profile, input.GroundLevelHeight, envelope.Height - input.GroundLevelHeight,
                                        Vector3.ZAxis, 0.0, envelope.Transform, null, envelope.Representation,
                                        Guid.NewGuid(), "");
                levels.AddRange(LevelMaker.MakeLevels(envelope, input.StandardLevelHeight));
            }
            else
            {
                levels.Add(LevelMaker.MakeLevel(envelope, envelope.Height));
            }

            // Add mechanical level and roof level to highest Envelope.
            envelope = envelopes.Last();
            levels.Add(LevelMaker.MakeLevel(envelope, envelope.Elevation + envelope.Height));
            var mechHeight = input.StandardLevelHeight * input.MechanicalLevelHeightRatio;
            var level = LevelMaker.MakeLevel(envelopes.Last(), bldgHeight - mechHeight);
            if (level != null)
            {
                levels.Add(level);
            }

            // Create temporary envelope to populate region beneath mechanical level.
            envelope = new Envelope(envelopes.Last().Profile.Perimeter,
                                    envelopes.Last().Elevation,
                                    envelopes.Last().Height - mechHeight,
                                    Vector3.ZAxis,
                                    0.0,
                                    envelopes.Last().Transform,
                                    null,
                                    envelopes.Last().Representation,
                                    Guid.NewGuid(),
                                    "");
            levels.AddRange(LevelMaker.MakeLevels(envelope, input.StandardLevelHeight, false).Skip(1));

            // Remove completed Levels from Envelope list.
            envelopes = envelopes.Where(e => e.Elevation >= 0.0).Skip(1).ToList();
            envelopes.Reverse();
            envelopes = envelopes.Skip(1).ToList();
            envelopes.Reverse();

            //// Add standard height Levels.
            foreach (var env in envelopes)
            {
                levels.AddRange(LevelMaker.MakeLevels(env, input.StandardLevelHeight).Skip(1).ToList());
            }

            levels = levels.OrderBy(l => l.Elevation).ToList();
            var levelArea = 0.0;
            foreach (var lvl in levels)
            {
                levelArea += lvl.Perimeter.Area();
            }
            var matl = new Material(new Color(0.5f, 0.5f, 0.5f, 0.5f), 0.0f, 0.0f, Guid.NewGuid(), "Level");
            var output = new LevelsByEnvelopeOutputs(input.GroundLevelHeight, 5.0, levelArea); // mechHeight, levelArea);
            output.model.AddElements(levels);     
            foreach (var item in levels)
            {
                output.model.AddElement(new Panel(item.Perimeter, matl, new Transform(0.0, 0.0, item.Elevation), null, Guid.NewGuid(), ""));
            }
            return output;
        }
    }
}