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


            var rentable = new List<Envelope>();
            var subgrade = new List<Envelope>();
            var levels = new List<Level>();

            var bldgHeight = 0.0;
            var remnHeight = 0.0;
            
            foreach (var envelope in envelopes)
            {
                if (envelope.Elevation >= 0.0)
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

            var makeLevels = new MakeLevels(rentable);

            ////Add subgrade level.
            //var level = makeLevels.MakeLevel(subgrade.First().Elevation);
            //if (level != null)
            //{
            //    levels.Add(level);
            //}

            //// Add ground level.
            //level = makeLevels.MakeLevel(0.0);
            //if (level != null)
            //{
            //    levels.Add(level);
            //}

            // Add top level.
            var level = makeLevels.MakeLevel(bldgHeight);
            if (level != null)
            {
                levels.Add(level);
            }

            //// Add second level.
            //if (bldgHeight >= input.GroundLevelHeight)
            //{
            //    level = makeLevels.MakeLevel(input.GroundLevelHeight);
            //    if (level != null)
            //    {
            //        levels.Add(level);
            //        remnHeight = bldgHeight - input.GroundLevelHeight;
            //    }
            //}

            //// Add mechanical level.
            //var mechHeight = input.StandardLevelHeight * input.MechanicalLevelHeightRatio;
            //if (remnHeight >= mechHeight)
            //{
            //    level = makeLevels.MakeLevel(bldgHeight - mechHeight);
            //    if (level != null)
            //    {
            //        levels.Add(level);
            //        remnHeight -= mechHeight;
            //    }
            //}

            /// 
            //Add higher top occupied floor to accommodate piping under top mechanical floor.
            //if (remnHeight > input.StandardLevelHeight + 0.3)
            //{
            //    level = makeLevels.MakeLevel(bldgHeight - mechHeight - input.StandardLevelHeight + 0.3);
            //    if (level != null)
            //    {
            //        levels.Add(level);
            //        remnHeight -= input.StandardLevelHeight + 0.3;
            //    }
            //}

            // Add standard height levels.
            //var stdHeight = 0.0;
            //if (remnHeight >= input.StandardLevelHeight)
            //{
            //    var lvlQty = Math.Floor(remnHeight / input.StandardLevelHeight);
            //    var lvlElev = input.GroundLevelHeight + input.StandardLevelHeight;

            //    for (int i = 0; i < lvlQty; i++)
            //    {
            //        level = makeLevels.MakeLevel(lvlElev);
            //        if (level != null)
            //        {
            //            levels.Add(level);
            //        }
            //        lvlElev += stdHeight;
            //    }
            //}

            levels = levels.OrderBy(l => l.Elevation).ToList();

            var levelArea = 0.0;
            foreach (var item in levels)
            {
                levelArea += item.Perimeter.Area();
            }

            var matl = new Material(new Color(0.5f, 0.5f, 0.5f, 0.5f), 0.0f, 0.0f, Guid.NewGuid(), "Level");
            var output = new LevelsByEnvelopeOutputs(input.GroundLevelHeight, 5.0, 5.0, levelArea); //stdHeight, mechHeight
            output.model.AddElements(levels);     
            
            foreach (var item in levels)
            {
                output.model.AddElement(new Panel(item.Perimeter, matl, new Transform(0.0, 0.0, item.Elevation), null, Guid.NewGuid(), ""));
            }
            return output;
        }
    }
}