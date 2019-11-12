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
            envelopes = envelopes.OrderBy(e => e.Elevation).ToList();

            var levels = new List<Level>();

            var bldgHeight = 0.0;
            var remnHeight = 0.0;
            
            foreach (var envelope in envelopes)
            {
                if (envelope.Elevation >= 0.0)
                {
                    bldgHeight += envelope.Height;
                    remnHeight += envelope.Height;
                }
            }
            var makeLevels = new MakeLevels(envelopes);
            var levelArea = 0.0;

            // Add subgrade Level.
            var level = makeLevels.MakeLevel(envelopes.First().Elevation);
            if (level != null)
            {
                levels.Add(level);
                levelArea += level.Perimeter.Area();
            }

            // Add ground Level.
            level = makeLevels.MakeLevel(0.0);
            if (level != null)
            {
                levels.Add(level);
                levelArea += level.Perimeter.Area();
            }

            // Add top Level.
            level = makeLevels.MakeLevel(bldgHeight);
            if (level != null)
            {
                levels.Add(level);
                levelArea += level.Perimeter.Area();
            }

            //// Add second Level.
            if (bldgHeight >= input.GroundLevelHeight)
            {
                level = makeLevels.MakeLevel(input.GroundLevelHeight);
                if (level != null)
                {
                    levels.Add(level);
                    levelArea += level.Perimeter.Area();
                    remnHeight = bldgHeight - input.GroundLevelHeight;
                }
            }

            // Add mechanical Level.
            var mechHeight = input.StandardLevelHeight * input.MechanicalLevelHeightRatio;
            if (remnHeight >= mechHeight)
            {
                level = makeLevels.MakeLevel(bldgHeight - mechHeight);
                if (level != null)
                {
                    levels.Add(level);
                    levelArea += level.Perimeter.Area();
                    remnHeight -= mechHeight;
                }
            }

            // Add standard height Levels.
            var lvlQty = Math.Floor(remnHeight / input.StandardLevelHeight) - 1;
            var stdHeight = remnHeight / lvlQty;
            if (remnHeight >= input.StandardLevelHeight)
            {
                var lvlElev = input.GroundLevelHeight + input.StandardLevelHeight;
                for (int i = 0; i < lvlQty; i++)
                {
                    level = makeLevels.MakeLevel(lvlElev);
                    if (level != null)
                    {
                        levels.Add(level);
                        levelArea += level.Perimeter.Area();
                    }
                    lvlElev += stdHeight;
                }
            }
            levels = levels.OrderBy(l => l.Elevation).ToList();
            var matl = new Material(new Color(0.5f, 0.5f, 0.5f, 0.5f), 0.0f, 0.0f, Guid.NewGuid(), "Level");
            var output = new LevelsByEnvelopeOutputs(input.GroundLevelHeight, stdHeight, mechHeight, levelArea);
            output.model.AddElements(levels);     
            foreach (var item in levels)
            {
                output.model.AddElement(new Panel(item.Perimeter, matl, new Transform(0.0, 0.0, item.Elevation), null, Guid.NewGuid(), ""));
            }
            return output;
        }
    }
}