using System.Collections.Generic;
using System.Linq;
using System;
using Elements;
using Elements.Geometry;
using Elements.Geometry.Solids;

namespace EnvelopeBySketch
{
    public static class EnvelopeBySketch
    {
        /// <summary>
        /// Generates a building Envelope from a sketch of the footprint, a building height, and a setback configuration.
        /// </summary>
        /// <param name="model">The input model.</param>
        /// <param name="input">The arguments to the execution.</param>
        /// <returns>A EnvelopeBySketchOutputs instance containing computed results and the model with any new elements.</returns>
        public static EnvelopeBySketchOutputs Execute(Dictionary<string, Model> inputModels, EnvelopeBySketchInputs input)
        {
            Elements.Geometry.Solids.Extrude extrude;
            Representation geomRep;
            var envelopes = new List<Envelope>();
            var envMatl = new Material("envelope", new Color(0.3, 0.7, 0.7, 0.6), 0.0f, 0.0f);
            double buildingHeight = 0;
            double foundationDepth = 0;
            double envelopeElevation = 0;
            var output = new EnvelopeBySketchOutputs();

            var polygon = input.Perimeter;
            if (polygon.IsClockWise())
            {
                polygon = polygon.Reversed();
            }
            var envelopeProfile = polygon;
            double tiers = 1;
            if (inputModels.TryGetValue("Levels", out var levelsModel))
            {
                if (levelsModel.AllElementsOfType<Level>().Count() == 0)
                {
                    output.Errors.Add($"No Levels found in the model 'Levels'. Check the output from the function upstream that has a model output 'Levels'.");
                    return output;
                }
                var levels = levelsModel.AllElementsOfType<Level>().ToList();
                levels.Sort((l1, l2) => l1.Elevation.CompareTo(l2.Elevation));
                envelopeElevation = levels.First().Elevation;
                if (envelopeElevation < 0)
                {
                    foundationDepth = -envelopeElevation;
                }
                buildingHeight = levels.Last().Elevation + (levels.Last().Height ?? 0);
                output.Model.AddElements(CreateLevelVolumes(levels, envelopeProfile));
            }
            else
            {
                // Create the foundation Envelope.
                if (input.FoundationDepth > 0)
                {
                    extrude = new Extrude(input.Perimeter, input.FoundationDepth, Vector3.ZAxis, false);
                    geomRep = new Representation(new List<SolidOperation>() { extrude });
                    var elevation = envelopeElevation - input.FoundationDepth;
                    envelopes.Add(new Envelope(input.Perimeter, elevation, input.FoundationDepth, Vector3.ZAxis, 0.0, null,
                                  new Transform(0.0, 0.0, elevation), envMatl, geomRep, false, Guid.NewGuid(), ""));
                }
                buildingHeight = input.BuildingHeight;
                foundationDepth = input.FoundationDepth;
                // Create the Envelope at the location's zero plane.
                tiers = input.UseSetbacks ? Math.Floor(buildingHeight / input.SetbackInterval) : 1;
            }
            var tierHeight = tiers > 1 ? buildingHeight / tiers : buildingHeight;

            extrude = new Extrude(polygon, tierHeight, Vector3.ZAxis, false);
            geomRep = new Representation(new List<SolidOperation>() { extrude });
            envelopes.Add(new Envelope(input.Perimeter, envelopeElevation, tierHeight, Vector3.ZAxis, 0.0, null,
                          new Transform(0, 0, envelopeElevation), envMatl, geomRep, false, Guid.NewGuid(), ""));
            // Create the remaining Envelope Elements.
            var offsFactor = -1;
            var elevFactor = 1;
            for (int i = 0; i < tiers - 1; i++)
            {
                var tryPer = input.Perimeter.Offset(input.SetbackDepth * offsFactor);
                tryPer = tryPer.OrderByDescending(p => p.Area()).ToArray();
                if (tryPer.Count() == 0 || tryPer.First().Area() < input.MinimumTierArea)
                {
                    break;
                }
                polygon = tryPer.First();
                if (polygon.IsClockWise())
                {
                    polygon = polygon.Reversed();
                }
                extrude = new Extrude(polygon, tierHeight, Vector3.ZAxis, false);
                geomRep = new Representation(new List<SolidOperation>() { extrude });
                var elevation = envelopeElevation + (tierHeight * elevFactor);
                envelopes.Add(new Envelope(tryPer.First(), elevation, tierHeight, Vector3.ZAxis, 0.0, null,
                              new Transform(0.0, 0.0, elevation), envMatl, geomRep, false, Guid.NewGuid(), ""));
                offsFactor--;
                elevFactor++;
            }
            envelopes = envelopes.OrderBy(e => e.Elevation).ToList();
            foreach (var env in envelopes)
            {
                output.Model.AddElement(env);
            }

            output.Height = buildingHeight;
            output.Subgrade = foundationDepth;
            return output;
        }

        private static IEnumerable<LevelVolume> CreateLevelVolumes(IEnumerable<Level> levels, Profile profile)
        {
            List<LevelVolume> result = new List<LevelVolume>();
            var profileWithZeroElevation = profile.Project(new Plane(Vector3.Origin, Vector3.ZAxis));

            foreach (var level in levels)
            {
                if (!level.Height.HasValue)
                {
                    continue;
                }
                var levelVolume = new LevelVolume()
                {
                    Profile = profileWithZeroElevation,
                    Height = level.Height.Value,
                    Area = profileWithZeroElevation.Area(),
                    Transform = new Transform(0, 0, level.Elevation),
                    Material = BuiltInMaterials.Glass,
                    Representation = new Extrude(profile, level.Height.Value, Vector3.ZAxis, false),
                    Name = level.Name
                };
                result.Add(levelVolume);
            }

            return result;
        }
    }
}