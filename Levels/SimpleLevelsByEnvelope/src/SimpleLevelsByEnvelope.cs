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

            var matl = BuiltInMaterials.Glass;
            matl.SpecularFactor = 0.5;
            matl.GlossinessFactor = 0.0;

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
                var nonExtrudeEnvelope = envelope.Representation.SolidOperations.First().GetType() != typeof(Extrude);

                var envSubGrade = subgradeLevels.Where(l => l.Elevation < max && l.Elevation >= min).ToList();
                for (int i = 0; i < envSubGrade.Count(); i++)
                {
                    var l = envSubGrade[i];
                    if (nonExtrudeEnvelope)
                    {
                        try
                        {
                            var elev = l.Elevation;
                            if (elev == 0)
                            {
                                elev += 0.01;
                            }
                            envelope.Representation.SolidOperations.First().Solid.Intersects(new Plane((0, 0, elev), Vector3.ZAxis), out var polygons);
                            if (polygons != null)
                            {
                                envelopeProfile = new Profile(polygons.OrderBy(p => p.Area()).Last(), new List<Polygon>()).Project(new Plane((0, 0), Vector3.ZAxis));
                            }
                        }
                        catch
                        {
                            // keep envelope profile as-is
                        }
                    }
                    var levelAbove = i == 0 ? aboveGradeLevels.First() : envSubGrade[i - 1];
                    var subGradePerimeter = new LevelPerimeter(envelopeProfile.Area(), l.Elevation, envelopeProfile.Perimeter, Guid.NewGuid(), l.Name);
                    levelPerimeters.Add(subGradePerimeter);
                    subGradeArea += subGradePerimeter.Area;
                    areaTotal += subGradePerimeter.Area;

                    var levelHeight = levelAbove.Elevation - l.Elevation;
                    var representation = new Representation(new SolidOperation[] { new Extrude(envelopeProfile, levelHeight, Vector3.ZAxis, false) });
                    var subGradeVolume = new LevelVolume(envelopeProfile, levelHeight, envelopeProfile.Area(), envelope.Name, new Transform(0, 0, l.Elevation), matl, representation, false, Guid.NewGuid(), l.Name);
                    subGradeVolume.AdditionalProperties["Envelope"] = envelope.Id;
                    var scopeName = subGradeVolume.Name;
                    if (!String.IsNullOrEmpty(subGradeVolume.BuildingName))
                    {
                        scopeName = $"{subGradeVolume.BuildingName}: {scopeName}";
                    }
                    var bbox = new BBox3(subGradeVolume);
                    bbox.Max = bbox.Max + (0, 0, -1);
                    var scope = new ViewScope(
                       bbox,
                        new Camera(default(Vector3), CameraNamedPosition.Top, CameraProjection.Orthographic),
                        true,
                        name: scopeName);
                    subGradeVolume.AdditionalProperties["Scope"] = scope;
                    scopes.Add(scope);
                    levelVolumes.Add(subGradeVolume);
                }
                if (envSubGrade.Count > 0)
                { // if this was a subgrade envelope, let's not add anything else.
                    continue;
                }
                // var envAboveGrade = aboveGradeLevels.Where(l => l.Elevation < max - minLevelHeight && l.Elevation >= min).ToList();
                // We want to make sure we start a level at the very base of the envelope. 
                var aboveGradeLevelsWithinEnvelope = aboveGradeLevels.Where(l => l.Elevation > min + minLevelHeight && l.Elevation < max - minLevelHeight).ToList();
                var nameForMin = aboveGradeLevels.LastOrDefault(l => l.Elevation < min + minLevelHeight)?.Name ?? "";
                var nameForMax = aboveGradeLevels.FirstOrDefault(l => l.Elevation > max - minLevelHeight)?.Name ?? "";
                for (int i = -1; i < aboveGradeLevelsWithinEnvelope.Count(); i++)
                {
                    var name = nameForMin;
                    if (i > -1 && i < aboveGradeLevelsWithinEnvelope.Count)
                    {
                        name = aboveGradeLevelsWithinEnvelope[i].Name;
                    }

                    var levelElevation = i == -1 ? min : aboveGradeLevelsWithinEnvelope[i].Elevation;
                    if (nonExtrudeEnvelope)
                    {
                        try
                        {
                            var elev = levelElevation;
                            if (elev == 0)
                            {
                                elev += 0.01;
                            }
                            envelope.Representation.SolidOperations.First().Solid.Intersects(new Plane((0, 0, elev), Vector3.ZAxis), out var polygons);
                            if (polygons != null)
                            {
                                envelopeProfile = new Profile(polygons.OrderBy(p => p.Area()).Last(), new List<Polygon>()).Project(new Plane((0, 0), Vector3.ZAxis));
                            }
                        }
                        catch
                        {
                            // keep envelope profile as-is
                        }
                    }
                    var nextLevelElevation = i == aboveGradeLevelsWithinEnvelope.Count - 1 ? max : aboveGradeLevelsWithinEnvelope[i + 1].Elevation;
                    if (nextLevelElevation > max - minLevelHeight)
                    {
                        nextLevelElevation = max;
                    }
                    levelPerimeters.Add(new LevelPerimeter(envelopeProfile.Area(), levelElevation, envelopeProfile.Perimeter, Guid.NewGuid(), name));
                    aboveGradeArea += envelopeProfile.Area();
                    areaTotal += aboveGradeArea;

                    var levelHeight = nextLevelElevation - levelElevation;
                    var newProfile = envelopeProfile;
                    try
                    {
                        var profileOffset = envelopeProfile.Perimeter.Offset(-0.1);
                        newProfile = new Profile(profileOffset[0], envelopeProfile.Voids, Guid.NewGuid(), "Level volume representation");
                    }
                    catch
                    {

                    }
                    var representation = new Extrude(newProfile, levelHeight, Vector3.ZAxis, false);
                    var volume = new LevelVolume(envelopeProfile, levelHeight, envelopeProfile.Area(), envelope.Name, new Transform(0, 0, levelElevation), matl, representation, false, Guid.NewGuid(), name);
                    volume.AdditionalProperties["Envelope"] = envelope.Id;
                    var bbox = new BBox3(volume);
                    bbox.Max = bbox.Max + (0, 0, -1);
                    bbox.Min = bbox.Min + (0, 0, -0.3);
                    var scopeName = volume.Name;
                    if (!String.IsNullOrEmpty(volume.BuildingName))
                    {
                        scopeName = $"{volume.BuildingName}: {scopeName}";
                    }
                    var scope = new ViewScope(bbox, new Camera(default(Vector3), CameraNamedPosition.Top, CameraProjection.Orthographic), true, name: scopeName);
                    volume.AdditionalProperties["Scope"] = scope;
                    scopes.Add(scope);
                    levelVolumes.Add(volume);

                }

                // Add a roof perimeter so floors are created, but don't count roof area

                levelPerimeters.Add(new LevelPerimeter(envelopeProfile.Area(), max, envelopeProfile.Perimeter, Guid.NewGuid(), "Roof"));

            }

            var output = new SimpleLevelsByEnvelopeOutputs(levels.Count, areaTotal, subGradeArea, aboveGradeArea);
            output.Model.AddElements(scopes);
            output.Model.AddElements(levels.OrderByDescending(l => l.Elevation));
            output.Model.AddElements(levelPerimeters);
            output.Model.AddElements(levelVolumes);

            foreach (var levelPerimeter in levelPerimeters)
            {
                output.Model.AddElement(new Panel(levelPerimeter.Perimeter.Project(new Plane(Vector3.Origin, Vector3.ZAxis)), matl, new Transform(0.0, 0.0, levelPerimeter.Elevation),
                                        null, false, Guid.NewGuid(), levelPerimeter.Name));
            }
            return output;
        }
    }
}