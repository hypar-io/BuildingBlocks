using Elements;
using Elements.Geometry;
using Elements.Geometry.Solids;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SimpleLevelsByEnvelope
{
    public static class SimpleLevelsByEnvelope
    {
        /// <summary>
        /// Creates Levels and LevelPerimeters from an incoming Envelope and height arguments.
        /// </summary>
        /// <param name="model">The input model.</param>
        /// <param name="input">The arguments to the execution.</param>
        /// <returns>A LevelsByEnvelopeOutputs instance containing computed results and the model with any new elements.</returns>
        public static SimpleLevelsByEnvelopeOutputs Execute(Dictionary<string, Model> inputModels, SimpleLevelsByEnvelopeInputs input)
        {
            inputModels.TryGetValue("Envelope", out var model);
            if (model == null || model.AllElementsOfType<Envelope>().Count() == 0)
            {
                throw new ArgumentException("No Envelope found.");
            }
            var envelopes = model.AllElementsOfType<Envelope>();
            CorrectEnvelopeHeightsAndElevations(envelopes);
            var envelopesOrdered = envelopes.OrderBy(e => e.Elevation);
            var topHeightsOrdered = envelopes.Select(e => e.Elevation + e.Height).OrderBy(e => e);

            var minElevation = envelopesOrdered.Select(e => e.Elevation).First();
            var maxElevation = topHeightsOrdered.Last();
            var minLevelHeight = 3.0;
            var levels = new List<Level>();
            var grade = envelopesOrdered.First(e => e.Elevation + e.Height > 0).Elevation;

            var currentLevel = grade;
            var heightIndex = 0;

            // base and normal levels
            while (currentLevel < maxElevation - input.TopLevelHeight - minLevelHeight)
            {
                Console.WriteLine($"Adding Level {levels.Count + 1:0} at elevation {currentLevel}");
                levels.Add(new Level(currentLevel, Guid.NewGuid(), $"Level {levels.Count + 1:0}"));
                currentLevel += input.BaseLevels[Math.Min(heightIndex++, input.BaseLevels.Count - 1)];
            }

            // penthouse + roof level
            levels.Add(new Level(maxElevation - input.TopLevelHeight, Guid.NewGuid(), "Penthouse Level"));
            levels.Add(new Level(maxElevation, Guid.NewGuid(), "Roof Level"));

            if (minElevation < grade)
            {
                // subgrade levels
                currentLevel = -input.SubgradeLevelHeight;
                var subgradeLevelCounter = 1;
                while (currentLevel > minElevation + input.SubgradeLevelHeight)
                {
                    levels.Add(new Level(currentLevel, Guid.NewGuid(), $"Level B{subgradeLevelCounter:0}"));
                    currentLevel -= input.SubgradeLevelHeight;
                    subgradeLevelCounter++;
                }
                levels.Add(new Level(minElevation, Guid.NewGuid(), $"Level B{subgradeLevelCounter:0}"));
            }

            var levelMaterial = BuiltInMaterials.Glass;
            levelMaterial.SpecularFactor = 0.5;
            levelMaterial.GlossinessFactor = 0.0;

            // construct level perimeters and calculate areas

            var levelPerimeters = new List<LevelPerimeter>();
            var levelVolumes = new List<LevelVolume>();
            var scopes = new List<ViewScope>();
            var areaTotal = 0.0;
            var subGradeArea = 0.0;
            var aboveGradeArea = 0.0;

            var subgradeLevels = levels.Where(l => l.Elevation < grade);
            var aboveGradeLevels = levels.Where(l => l.Elevation >= grade).OrderBy(l => l.Elevation);

            foreach (var envelope in envelopes)
            {
                var envelopeRepresentation = envelope.Representation;
                var min = envelope.Elevation;
                var max = envelope.Elevation + envelope.Height;
                var envelopeProfile = envelope.Profile;
                var hasAnyNonExtrudeEnvelopes = envelope.Representation.SolidOperations.Any(so => so.GetType() != typeof(Extrude));

                var envSubGrade = subgradeLevels.Where(l => l.Elevation < max && l.Elevation >= min).ToList();
                for (int i = 0; i < envSubGrade.Count(); i++)
                {
                    var l = envSubGrade[i];
                    var levelAbove = i == 0 ? aboveGradeLevels.First() : envSubGrade[i - 1];
                    var levelHeight = levelAbove.Elevation - l.Elevation;
                    ProcessSingleLevel(l.Elevation, l.Name, levelHeight, hasAnyNonExtrudeEnvelopes, envelope, levelMaterial, scopes, levelVolumes, ref envelopeProfile, ref subGradeArea, ref areaTotal, ref levelPerimeters);
                }
                if (envSubGrade.Count > 0)
                { // if this was a subgrade envelope, let's not add anything else.
                    continue;
                }
                // We want to make sure we start a level at the very base of the envelope. 
                var aboveGradeLevelsWithinEnvelope = aboveGradeLevels.Where(l => l.Elevation > min + minLevelHeight && l.Elevation < max - minLevelHeight).ToList();
                var nameForMin = aboveGradeLevels.LastOrDefault(l => l.Elevation < min + minLevelHeight)?.Name ?? "";
                for (int i = -1; i < aboveGradeLevelsWithinEnvelope.Count(); i++)
                {
                    var name = nameForMin;
                    if (i > -1 && i < aboveGradeLevelsWithinEnvelope.Count)
                    {
                        name = aboveGradeLevelsWithinEnvelope[i].Name;
                    }

                    var levelElevation = i == -1 ? min : aboveGradeLevelsWithinEnvelope[i].Elevation;
                    var nextLevelElevation = i == aboveGradeLevelsWithinEnvelope.Count - 1 ? max : aboveGradeLevelsWithinEnvelope[i + 1].Elevation;
                    if (nextLevelElevation > max - minLevelHeight)
                    {
                        nextLevelElevation = max;
                    }
                    var levelHeight = nextLevelElevation - levelElevation;
                    ProcessSingleLevel(levelElevation, name, levelHeight, hasAnyNonExtrudeEnvelopes, envelope, levelMaterial, scopes, levelVolumes, ref envelopeProfile, ref aboveGradeArea, ref areaTotal, ref levelPerimeters);
                    // levelPerimeters.Add(new LevelPerimeter(envelopeProfile.Area(), levelElevation, envelopeProfile.Perimeter, Guid.NewGuid(), name));
                    // aboveGradeArea += envelopeProfile.Area();
                    // areaTotal += aboveGradeArea;

                    // var newProfile = envelopeProfile;
                    // try
                    // {
                    //     var profileOffset = envelopeProfile.Perimeter.Offset(-0.1);
                    //     newProfile = new Profile(profileOffset[0], envelopeProfile.Voids, Guid.NewGuid(), "Level volume representation");
                    // }
                    // catch
                    // {

                    // }
                    // var representation = new Extrude(newProfile, levelHeight, Vector3.ZAxis, false);
                    // var volume = new LevelVolume(envelopeProfile, levelHeight, envelopeProfile.Area(), envelope.Name, new Transform(0, 0, levelElevation), levelMaterial, representation, false, Guid.NewGuid(), name);
                    // volume.AdditionalProperties["Envelope"] = envelope.Id;
                    // var bbox = new BBox3(volume);
                    // // drop the box by a meter to avoid ceilings / beams, etc.
                    // bbox.Max += (0, 0, -1);
                    // // drop the bottom to encompass floors below
                    // bbox.Min += (0, 0, -0.3);
                    // var scopeName = volume.Name;
                    // if (!String.IsNullOrEmpty(volume.BuildingName))
                    // {
                    //     scopeName = $"{volume.BuildingName}: {scopeName}";
                    // }
                    // var scope = new ViewScope(bbox, new Camera(default(Vector3), CameraNamedPosition.Top, CameraProjection.Orthographic), true, name: scopeName);
                    // volume.AdditionalProperties["Plan View"] = scope;
                    // scopes.Add(scope);
                    // levelVolumes.Add(volume);

                }

            }

            var output = new SimpleLevelsByEnvelopeOutputs(levels.Count, areaTotal, subGradeArea, aboveGradeArea);
            output.Model.AddElements(scopes);
            output.Model.AddElements(levels.OrderByDescending(l => l.Elevation));
            output.Model.AddElements(levelPerimeters);
            output.Model.AddElements(levelVolumes);

            foreach (var levelPerimeter in levelPerimeters)
            {
                output.Model.AddElement(new Panel(levelPerimeter.Perimeter.Project(new Plane(Vector3.Origin, Vector3.ZAxis)), levelMaterial, new Transform(0.0, 0.0, levelPerimeter.Elevation),
                                        null, false, Guid.NewGuid(), levelPerimeter.Name));
            }
            return output;
        }

        private static void ProcessSingleLevel(double levelElevation, string levelName, double levelHeight, bool hasAnyNonExtrudeEnvelopes, Envelope envelope, Material levelMaterial, List<ViewScope> scopes, List<LevelVolume> levelVolumes, ref Profile envelopeProfile, ref double areaGroup, ref double areaTotal, ref List<LevelPerimeter> levelPerimeters)
        {
            Representation representation = null;
            var levelArea = 0.0;
            var successfullyCreatedNonExtrudeEnvelopes = false;
            if (hasAnyNonExtrudeEnvelopes)
            {
                try
                {
                    var solidOps = new List<SolidOperation>();
                    representation = new Representation(solidOps)
                    {
                        SkipCSGUnion = true
                    };
                    foreach (var solidOp in envelope.Representation.SolidOperations)
                    {
                        // TODO — handle solid operations containing voids or overlapping solids
                        if (solidOp.IsVoid)
                        {
                            continue;
                        }
                        // attempt to get intersection at level. If this fails, try slightly higher, and if that fails, try slightly lower.
                        if (solidOp.Solid.Intersects(new Plane((0, 0, levelElevation), Vector3.ZAxis), out List<Polygon> polygons))
                        {

                        }
                        else if (solidOp.Solid.Intersects(new Plane((0, 0, levelElevation + 0.01), Vector3.ZAxis), out polygons))
                        {

                        }
                        else if (solidOp.Solid.Intersects(new Plane((0, 0, levelElevation - 0.01), Vector3.ZAxis), out polygons))
                        {

                        }

                        if (polygons != null && polygons.Count > 0)
                        {
                            var profiles = Profile.CreateFromPolygons(polygons);
                            envelopeProfile = profiles.OrderBy(p => p.Area()).Last();
                            var extrudes = profiles.Select(p => new Extrude(p, levelHeight, Vector3.ZAxis, false)).ToArray();
                            solidOps.AddRange(extrudes);
                            foreach (var profile in profiles)
                            {
                                var profileArea = profile.Area();
                                areaGroup += profileArea;
                                levelArea += profileArea;
                                areaTotal += profileArea;
                                var subGradePerimeter = new LevelPerimeter(profile.Area(), levelElevation, profile.Perimeter, Guid.NewGuid(), levelName);
                                levelPerimeters.Add(subGradePerimeter);
                            }
                            successfullyCreatedNonExtrudeEnvelopes = true;
                        }
                    }
                }
                catch
                {
                    // we pass through — the `successfullyCreatedNonExtrudeEnvelopes` variable will be false and we'll fall back to the simple extrude from one profile.
                }
            }

            if (!hasAnyNonExtrudeEnvelopes || !successfullyCreatedNonExtrudeEnvelopes)
            {
                representation = new Extrude(envelopeProfile, levelHeight, Vector3.ZAxis, false);
                var subGradePerimeter = new LevelPerimeter(envelopeProfile.Area(), levelElevation, envelopeProfile.Perimeter, Guid.NewGuid(), levelName);
                levelPerimeters.Add(subGradePerimeter);
                areaGroup += subGradePerimeter.Area;
                areaTotal += subGradePerimeter.Area;
                levelArea = envelopeProfile.Area();
            }

            var subGradeVolume = new LevelVolume(envelopeProfile, levelHeight, envelopeProfile.Area(), envelope.Name, new Transform(0, 0, levelElevation), levelMaterial, representation, false, Guid.NewGuid(), levelName);
            subGradeVolume.AdditionalProperties["Envelope"] = envelope.Id;
            var scopeName = subGradeVolume.Name;
            if (!String.IsNullOrEmpty(subGradeVolume.BuildingName))
            {
                scopeName = $"{subGradeVolume.BuildingName}: {scopeName}";
            }
            var bbox = new BBox3(subGradeVolume);
            // drop the box by a meter to avoid ceilings / beams, etc.
            bbox.Max += (0, 0, -1);
            // drop the bottom to encompass floors below
            bbox.Min += (0, 0, -0.3);
            var scope = new ViewScope(
               bbox,
                new Camera(default, CameraNamedPosition.Top, CameraProjection.Orthographic),
                true,
                name: scopeName);
            subGradeVolume.AdditionalProperties["Plan View"] = scope;
            scopes.Add(scope);
            levelVolumes.Add(subGradeVolume);
        }

        private static void CorrectEnvelopeHeightsAndElevations(IEnumerable<Envelope> envelopes)
        {
            envelopes.ToList().ForEach(e =>
            {
                var bbox = new BBox3(e);
                e.Elevation = bbox.Min.Z;
                var height = bbox.Max.Z - bbox.Min.Z;
                e.Height = height;
            });
        }
    }
}