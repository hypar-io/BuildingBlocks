using Elements;
using Elements.Geometry;
using Elements.Geometry.Solids;
using Elements.Validators;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RoofFunction
{
    public static class RoofFunction
    {
        private const int minRoofArea = 5;

        /// <summary>
        /// The RoofFunction function.
        /// </summary>
        /// <param name="model">The input model.</param>
        /// <param name="input">The arguments to the execution.</param>
        /// <returns>A RoofFunctionOutputs instance containing computed results and the model with any new elements.</returns>
        public static RoofFunctionOutputs Execute(Dictionary<string, Model> inputModels, RoofFunctionInputs input)
        {
            var output = new RoofFunctionOutputs();
            
            if (input.RoofColor.HasValue)
            {
                Roof.RoofMaterial.Color = input.RoofColor.Value;
            }

            if (input.InsulationColor.HasValue)
            {
                Roof.InsulationMaterial.Color = input.InsulationColor.Value;
            }

            var hasFootprints = inputModels.TryGetValue("Masterplan", out var masterplanModel);
            var hasEnvelopes = inputModels.TryGetValue("Envelope", out var envelopeModel);
            var hasLevels = inputModels.TryGetValue("Levels", out var levelsModel);
            var hasConceptualMass = inputModels.TryGetValue("Conceptual Mass", out var massModel);
            var hasEnclosure = inputModels.TryGetValue("Enclosure", out var enclosureModel);
            if (!hasEnvelopes && !hasFootprints && !hasLevels && !hasConceptualMass)
            {
                output.Warnings.Add("There's nothing in the model from which to create a roof. Please add a conceptual mass, envelope, or levels.");
            }
            if (hasConceptualMass)
            {
                var conceptualMass = massModel.AllElementsOfType<ConceptualMass>();
                var levelVolumes = massModel.AllElementsOfType<LevelVolume>();
                if (hasEnclosure)
                {
                    var profiles = enclosureModel.AllElementsOfType<Profile>().Where(e => e.AdditionalProperties.ContainsKey("Level"));
                    foreach (var p in profiles)
                    {
                        var level = new Guid(p.AdditionalProperties["Level"] as string);
                        if (massModel.Elements.TryGetValue(level, out var lv))
                        {
                            var levelVol = lv as LevelVolume;
                            levelVol.Profile = p;
                        }
                    }
                }
                var levelVolumesByBuilding = levelVolumes.GroupBy(lv =>
                {
                    var env = lv.Mass ?? lv.Envelope;
                    var mass = massModel.Elements[env.Value] as ConceptualMass;
                    return mass.Building ?? lv.Mass ?? lv.Envelope;
                });
                foreach (var lvlVolumeGrp in levelVolumesByBuilding)
                {
                    var levelVolumesOrdered = lvlVolumeGrp.GroupBy(lv => lv.Transform.Origin.Z).OrderBy(g => g.Key);
                    ProcessLevelVolumes(input, output, levelVolumesOrdered);
                }
            }
            else if (hasFootprints)
            {
                var footprints = masterplanModel.AllElementsOfType<Footprint>();
                CreateRoofsFromElements(footprints, input, output);
            }
            else if (hasEnvelopes)
            {
                var envelopes = envelopeModel.AllElementsOfType<Envelope>();
                CreateRoofsFromElements(envelopes, input, output);
            }
            else if (hasLevels)
            {
                var levelVolumes = levelsModel?.AllElementsOfType<LevelVolume>();
                var projectedProfiles = Profile.UnionAll(levelVolumes.Select(lv => lv.Profile));
                foreach (var union in projectedProfiles)
                {
                    var levelVolumesWithinUnion = levelVolumes
                      .Where(lv => union.Contains(lv.Profile.Perimeter.PointInternal()))
                      .GroupBy(lv => lv.Transform.Origin.Z)
                      .OrderBy(grp => grp.Key);
                    ProcessLevelVolumes(input, output, levelVolumesWithinUnion);
                }
            }
            return output;
        }

        private static void ProcessLevelVolumes(RoofFunctionInputs input, RoofFunctionOutputs output, IOrderedEnumerable<IGrouping<double, LevelVolume>> profilesInUnion)
        {
            var keys = profilesInUnion.Select(g => g.Key).Reverse();
            var dict = profilesInUnion.ToDictionary(grp => grp.Key, grp => grp);
            List<Profile> previousProfiles = null;
            // counting down from top
            foreach (var elevation in keys)
            {
                var allLvs = dict[elevation];
                if (previousProfiles == null)
                {
                    previousProfiles = new List<Profile>();
                    foreach (var lv in allLvs)
                    {
                        var lvProfile = lv.Profile;
                        previousProfiles.Add(lvProfile);
                        var roofs = CreateRoofAndInsulation(lv.Profile, input, lv.Transform.Concatenated(new Transform(0, 0, lv.Height)));
                        if (lv.AdditionalProperties.TryGetValue("Envelope", out var envId))
                        {
                            roofs[0].AdditionalProperties.Add("Envelope", envId);
                        }
                        if (lv.AdditionalProperties.TryGetValue("Footprint", out var fpId))
                        {
                            roofs[0].AdditionalProperties.Add("Footprint", fpId);
                        }
                        output.Model.AddElements(roofs);
                    }
                }
                else
                {
                    var thisLevelprofiles = allLvs.Select(lv => lv.Profile);
                    List<Profile> difference = null;
                    try
                    {
                        difference = Profile.Difference(thisLevelprofiles, previousProfiles);
                    }
                    catch
                    {
                        // try again if exceptions occur
                        var offsetDistance = 0.01;
                        var aOffset = Profile.Offset(thisLevelprofiles, -offsetDistance);
                        var bOffset = Profile.Offset(previousProfiles, offsetDistance);
                        difference = Profile.Offset(Profile.Difference(aOffset, bOffset), offsetDistance);
                    }
                    if (difference != null && difference.Count > 0)
                    {
                        foreach (var profile in difference.Where(d => Math.Abs(d.Area()) > minRoofArea))
                        {
                            var firstLevel = allLvs.FirstOrDefault();
                            if(firstLevel == null) {
                                continue;
                            }
                            var roofs = CreateRoofAndInsulation(profile, input, firstLevel.Transform.Concatenated(new Transform(0, 0, firstLevel.Height)));
                            if (firstLevel.AdditionalProperties.TryGetValue("Envelope", out var envId))
                            {
                                roofs[0].AdditionalProperties.Add("Envelope", envId);
                            }
                            if (firstLevel.AdditionalProperties.TryGetValue("Footprint", out var fpId))
                            {
                                roofs[0].AdditionalProperties.Add("Footprint", fpId);
                            }
                            output.Model.AddElements(roofs);
                        }
                    }
                    previousProfiles = thisLevelprofiles.ToList();
                }
            }
        }

        private static void CreateRoofsFromElements<T>(IEnumerable<T> elements, RoofFunctionInputs input, RoofFunctionOutputs output) where T : GeometricElement
        {
            var allRoofFaces = GetAllRoofProfiles(elements)
                      .GroupBy(el => el.Perimeter.Start.Z)
                      .OrderBy(grp => grp.Key);
            var keys = allRoofFaces.Select(g => g.Key).Reverse();
            var dict = allRoofFaces.ToDictionary(grp => grp.Key, grp => grp);
            List<Profile> previousProfiles = null;
            foreach (var elevation in keys)
            {
                var roofFaces = dict[elevation];
                if (previousProfiles == null)
                {
                    previousProfiles = GetFlatRoofProfiles(roofFaces);

                    foreach (var roofFace in roofFaces)
                    {
                        var polygonTransform = roofFace.Perimeter.ToTransform();
                        var inverse = polygonTransform.Inverted();
                        var profile = new Profile(roofFace.Perimeter.TransformedPolygon(inverse), roofFace.Voids.Select(i => i.TransformedPolygon(inverse)).ToList());
                        var roofs = CreateRoofAndInsulation(profile, input, polygonTransform);
                        output.Model.AddElements(roofs);
                    }
                }
                else
                {
                    var firstPolygon = roofFaces.First().Perimeter;
                    var firstPolygonPlane = new Plane(firstPolygon.Vertices.First(), firstPolygon.Normal());
                    var thisLevelProfiles = GetFlatRoofProfiles(roofFaces);
                    IEnumerable<Profile> difference = null;
                    try
                    {
                        difference = Profile.Difference(thisLevelProfiles, previousProfiles);

                    }
                    catch
                    {
                        // in case of boolean failure, some last-ditch cleanup attempt
                        try
                        {
                            Validator.DisableValidationOnConstruction = true;
                            var diff = Profile.Difference(thisLevelProfiles, previousProfiles);
                            var offset = Profile.Offset(diff, -0.01);
                            var offsetOut = Profile.Offset(offset, 0.01);
                            difference = offsetOut;
                            Validator.DisableValidationOnConstruction = false;
                        }
                        catch
                        {
                            output.Warnings.Add("Something went wrong computing the geometry of the roof profiles.");
                            difference = thisLevelProfiles;
                        }
                    }
                    if (difference != null)
                    {
                        foreach (var profile in difference.Where(d => d.Area() > minRoofArea))
                        {
                            var flatProfileTransform = profile.Perimeter.ToTransform();
                            var profileTransform = profile.Perimeter.ProjectAlong(Vector3.ZAxis, firstPolygonPlane).ToTransform();
                            var transform = profileTransform.Concatenated(flatProfileTransform.Inverted());
                            var roofs = CreateRoofAndInsulation(profile, input, transform);
                            output.Model.AddElements(roofs);
                        }
                    }
                    previousProfiles.AddRange(thisLevelProfiles.ToList());
                }
            }
        }

        private static Roof[] CreateRoofAndInsulation(Profile profile, RoofFunctionInputs input, Transform polygonTransform)
        {
            List<Roof> roofs = new List<Roof>();
            var insulationTransform = polygonTransform.Moved(0, 0, input.RoofThickness);
            if (input.KeepRoofBelowEnvelope)
            {
                polygonTransform.Move(new Vector3(0, 0, -input.RoofThickness - input.InsulationThickness));
                insulationTransform.Move(new Vector3(0, 0, -input.RoofThickness - input.InsulationThickness));
            }
            if (profile.Perimeter.IsClockWise())
            {
                profile = new Profile(profile.Perimeter.Reversed(), profile.Voids);
                profile.OrientVoids();
            }
            roofs.Add(new Roof(profile, input.RoofThickness, polygonTransform, false));

            if (!input.InsulationThickness.ApproximatelyEquals(0))
            {
                var insulation = new Roof(profile, input.InsulationThickness, insulationTransform, true);
                insulation.Name = "Insulation";
                roofs.Add(insulation);
                roofs[1].AdditionalProperties.Add("Roof", roofs[0].Id);
            }
            return roofs.ToArray();
        }

        private static List<Profile> GetAllRoofProfiles(IEnumerable<GeometricElement> elements)
        {
            var resultFaces = new List<Profile>();

            foreach (var element in elements)
            {
                var solids = element.Representation.SolidOperations.Where(so => !so.IsVoid).Select(so => so.Solid);
                var roofFaces = solids.SelectMany(s => s.Faces.Where(f => f.Value.Outer.ToPolygon().Normal().Dot(Vector3.ZAxis) > 0.7).Select(f => f.Value));
                // Certain functions, like sketch masterplan, create their solids as a collection of stacked volumes with a very slight gap between them.
                // This is a workaround for bugs in the unions created by solid operations containing multiple solids.
                // We don't want to create "roof" elements in those gaps, so we look for cases where the potential roof face is very close to
                // another face, and ignore those.
                var bottomFaceCentroids = solids.SelectMany(s => s.Faces.Where(f => f.Value.Outer.ToPolygon().Normal().Dot(Vector3.ZAxis) < -0.7).Select(f => f.Value.Outer.ToPolygon().Centroid()));
                foreach (var roofFace in roofFaces)
                {
                    var polygon = roofFace.Outer.ToPolygon();
                    var centroid = polygon.Centroid();
                    if (!bottomFaceCentroids.Any(c => c.DistanceTo(centroid) < 0.5))
                    {
                        resultFaces.Add(new Profile(
                            roofFace.Outer.ToPolygon().TransformedPolygon(element.Transform),
                            roofFace.Inner?.Select(i => i.ToPolygon().TransformedPolygon(element.Transform)).ToList()));
                    }
                }
            }
            return resultFaces;
        }

        private static List<Profile> GetFlatRoofProfiles(IEnumerable<Profile> roofFaces)
        {
            var xyPlane = new Plane(Vector3.Origin, Vector3.ZAxis);
            return roofFaces.Select(rf => new Profile(rf.Perimeter, rf.Voids).Project(xyPlane)).ToList();
        }

    }
}