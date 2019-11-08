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

            var rentable = new List<Envelope>();
            var subgrade = new List<Envelope>();
            var levels = new List<Level>();

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
            var lamina = new Elements.Geometry.Solids.Lamina(perimeter);
            var geomRep = new Representation(new List<Elements.Geometry.Solids.SolidOperation>() { lamina });

            var lvlElev = 0.0;
            var floorArea = 0.0;

            // Add ground level
            levels.Add(new Level(Vector3.Origin, Vector3.ZAxis, lvlElev, new Transform(), BuiltInMaterials.Glass, geomRep, Guid.NewGuid(), ""));
            if (bldgHeight >= input.GroundLevelHeight)
            {
                lvlElev = input.GroundLevelHeight;
                levels.Add(new Level(Vector3.Origin, Vector3.ZAxis, lvlElev, new Transform(), BuiltInMaterials.Glass, geomRep, Guid.NewGuid(), ""));
                floorArea += perimeter.Area();
                remnHeight = bldgHeight - input.GroundLevelHeight;
            }

            // Add top mechanical floor.
            var mechHeight = input.StandardLevelHeight * input.MechanicalLevelHeightRatio;
            if (remnHeight >= mechHeight)
            {
                lvlElev = bldgHeight - mechHeight;
                levels.Add(new Level(Vector3.Origin, Vector3.ZAxis, lvlElev, new Transform(), BuiltInMaterials.Glass, geomRep, Guid.NewGuid(), ""));
                remnHeight -= mechHeight;
            }

            // Add higher top floor to accommodate piping under top mechanical floor.
            if (remnHeight > input.StandardLevelHeight + 0.3)
            {
                lvlElev = bldgHeight - mechHeight - input.StandardLevelHeight + 0.3;
                levels.Add(new Level(Vector3.Origin, Vector3.ZAxis, lvlElev, new Transform(), BuiltInMaterials.Glass, geomRep, Guid.NewGuid(), ""));
                remnHeight -= input.StandardLevelHeight + 0.3;
            }

            // Add standard height levels.
            lvlElev = input.GroundLevelHeight + input.StandardLevelHeight;
            remnHeight -= input.StandardLevelHeight;
            var lvlQty = Math.Floor(remnHeight / input.StandardLevelHeight);
            var stdHeight = remnHeight / lvlQty;
            for (int i = 0; i < lvlQty; i++)
            {
                levels.Add(new Level(Vector3.Origin, Vector3.ZAxis, lvlElev, new Transform(), BuiltInMaterials.Glass, geomRep, Guid.NewGuid(), ""));
                floorArea += perimeter.Area();
                lvlElev += stdHeight;
            }
            var output = new LevelsByEnvelopeOutputs(input.GroundLevelHeight, stdHeight, mechHeight);
            foreach (var level in levels)
            {
                var panel = new Panel(perimeter, BuiltInMaterials.Glass, new Transform(0.0, 0.0, level.Elevation), geomRep, Guid.NewGuid(), "");
                output.model.AddElement(panel);
            }           
            output.model.AddElements(levels);
            return output;
        }
    }
}