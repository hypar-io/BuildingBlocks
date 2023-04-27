using Elements;
using Elements.Geometry;
using System.Collections.Generic;
using System.Linq;

namespace FloorsBySketch
{
    public static class FloorsBySketch
    {

        private readonly static double DEFAULT_FLOOR_THICKNESS = Units.FeetToMeters(1);

        private readonly static double DEFAULT_FLOOR_TO_FLOOR_HEIGHT = Units.FeetToMeters(10);
        /// <summary>
        /// Create floors by drawing them manually.
        /// </summary>
        /// <param name="model">The input model.</param>
        /// <param name="input">The arguments to the execution.</param>
        /// <returns>A FloorsBySketchOutputs instance containing computed results and the model with any new elements.</returns>
        public static FloorsBySketchOutputs Execute(Dictionary<string, Model> inputModels, FloorsBySketchInputs input)
        {
            var floorMaterial = new Material("Concrete")
            {
                Color = (0.7, 0.7, 0.7, 1.0)
            };
            var floors = new List<Floor>();

            // deprecated pathway
            foreach (var floorInput in input.Floors)
            {
                var f = new Floor(floorInput.Boundary, floorInput.Thickness, new Transform(0, 0, floorInput.Elevation), floorMaterial);
                f.AdditionalProperties["Original Boundary"] = floorInput.Boundary;
                f.AdditionalProperties["Boundary"] = floorInput.Boundary;
                floors.Add(f);
            }

            if (input.Overrides?.Additions?.Floors != null)
            {
                foreach (var floor in input.Overrides.Additions.Floors)
                {
                    var boundary = floor.Value.Boundary;
                    var elevation = 0.0;
                    var floorsUnderThisFloor = floors.Where(f => f.Profile.Contains(boundary.Centroid()));
                    if (floorsUnderThisFloor.Any())
                    {
                        elevation = floorsUnderThisFloor.Max(f => f.GetTopOfSlabElevation()) + DEFAULT_FLOOR_TO_FLOOR_HEIGHT;
                    }
                    var f = new Floor(floor.Value.Boundary, DEFAULT_FLOOR_THICKNESS, new Transform(0, 0, elevation - DEFAULT_FLOOR_THICKNESS), floorMaterial);
                    f.AdditionalProperties["Original Boundary"] = floor.Value.Boundary;
                    f.AdditionalProperties["Boundary"] = floor.Value.Boundary;
                    f.AdditionalProperties["Creation Id"] = floor.Id;
                    floors.Add(f);
                    Identity.AddOverrideIdentity(f, floor);

                }
            }
            if (input.Overrides?.FloorProperties != null)
            {
                foreach (var floorPropertyOverride in input.Overrides.FloorProperties)
                {
                    var identityMatch = floors.FirstOrDefault(f => IdentityMatch(f, floorPropertyOverride));
                    if (identityMatch != null)
                    {
                        identityMatch.Thickness = floorPropertyOverride.Value.Thickness;
                        Identity.AddOverrideIdentity(identityMatch, floorPropertyOverride);
                    }
                }
            }

            if (input.Overrides?.FloorElevation != null)
            {
                foreach (var floorElevationOverride in input.Overrides.FloorElevation)
                {
                    var identityMatch = floors.FirstOrDefault(f => IdentityMatch(f, floorElevationOverride));
                    if (identityMatch != null)
                    {
                        identityMatch.Transform = floorElevationOverride.Value.Transform;
                        Identity.AddOverrideIdentity(identityMatch, floorElevationOverride);
                    }
                }
            }

            if (input.Overrides?.Floors != null)
            {
                foreach (var floorEditOverride in input.Overrides.Floors)
                {
                    var identityMatch = floors.FirstOrDefault(f => IdentityMatch(f, floorEditOverride));
                    if (identityMatch != null)
                    {
                        identityMatch.Profile = floorEditOverride.Value.Boundary;
                        identityMatch.AdditionalProperties["Boundary"] = floorEditOverride.Value.Boundary;
                        Identity.AddOverrideIdentity(identityMatch, floorEditOverride);
                    }
                }
            }
            var areaSum = floors.Sum(f => f.Profile.Area());
            var output = new FloorsBySketchOutputs(areaSum);
            var count = 1;
            foreach (var floorGrp in floors.OrderBy(f => f.Elevation).GroupBy(f => f.Elevation))
            {
                var letter = 'A';
                if (floorGrp.Count() == 1)
                {
                    floorGrp.First().Name = $"Level {count++}";
                }
                else
                {
                    foreach (var floor in floorGrp)
                    {
                        floor.Name = $"Level {count}{letter++}";
                    }
                    count++;
                }
            }

            output.Model.AddElements(floors);

            return output;
        }

        private static bool IdentityMatch(Floor f, IOverride floorPropertyOverride)
        {
            dynamic identity = floorPropertyOverride.GetIdentity();
            f.AdditionalProperties.TryGetValue("Creation Id", out var creationId);
            return creationId as string == identity.CreationId.ToString() ||
            creationId == null && f.Profile.Perimeter.Centroid().DistanceTo(identity.OriginalBoundary.Centroid()) < 0.001;
        }
    }
}