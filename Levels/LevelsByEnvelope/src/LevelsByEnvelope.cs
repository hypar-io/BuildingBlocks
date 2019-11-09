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

            var lvlElev = 0.0;

            // Add ground level
            levels.Add(new Level(Vector3.Origin, Vector3.ZAxis, 0.0, Guid.NewGuid(), ""));
            if (bldgHeight >= input.GroundLevelHeight)
            {
                lvlElev = input.GroundLevelHeight;
                levels.Add(new Level(Vector3.Origin, Vector3.ZAxis, lvlElev, Guid.NewGuid(), ""));
                remnHeight = bldgHeight - input.GroundLevelHeight;
            }

            // Add top mechanical floor.
            var mechHeight = input.StandardLevelHeight * input.MechanicalLevelHeightRatio;
            if (remnHeight >= mechHeight)
            {
                lvlElev = bldgHeight - mechHeight;
                levels.Add(new Level(Vector3.Origin, Vector3.ZAxis, lvlElev, Guid.NewGuid(), ""));
                remnHeight -= mechHeight;
            }

            // Add higher top floor to accommodate piping under top mechanical floor.
            if (remnHeight > input.StandardLevelHeight + 0.3)
            {
                lvlElev = bldgHeight - mechHeight - input.StandardLevelHeight + 0.3;
                levels.Add(new Level(Vector3.Origin, Vector3.ZAxis, lvlElev, Guid.NewGuid(), ""));
                remnHeight -= input.StandardLevelHeight + 0.3;
            }

            // Add standard height levels.
            lvlElev = input.GroundLevelHeight + input.StandardLevelHeight;
            remnHeight -= input.StandardLevelHeight;
            var lvlQty = Math.Floor(remnHeight / input.StandardLevelHeight);
            var stdHeight = remnHeight / lvlQty;
            var perimeter = rentable.First().Profile.Perimeter;
            
            var floorArea = 0.0;
            for (int i = 0; i < lvlQty; i++)
            {
                levels.Add(new Level(Vector3.Origin, Vector3.ZAxis, lvlElev, Guid.NewGuid(), ""));
                floorArea += perimeter.Area();
                lvlElev += stdHeight;
            }
            var output = new LevelsByEnvelopeOutputs(input.GroundLevelHeight, stdHeight, mechHeight);
            output.model.AddElements(levels);

            var extrude = new Elements.Geometry.Solids.Extrude(perimeter, 0.01, Vector3.ZAxis * -1, 0.0, false);
            var geomRep = new Representation(new List<Elements.Geometry.Solids.SolidOperation>() { extrude });
            //var geomRep = new Representation(new List<Elements.Geometry.Solids.SolidOperation>() { new Elements.Geometry.Solids.Lamina( perimeter) });
            var matl = new Material(new Color(1.0f, 1.0f, 1.0f, 0.1f), 0.5f, 0.0f, Guid.NewGuid(), "Level");

            foreach (var level in levels)
            {
                output.model.AddElement(new Panel(perimeter, matl, new Transform(0.0, 0.0, level.Elevation), geomRep, Guid.NewGuid(), ""));
            }

            return output;
        }
    }
}