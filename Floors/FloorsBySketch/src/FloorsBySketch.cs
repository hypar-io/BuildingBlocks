using Elements;
using Elements.Geometry;
using System.Collections.Generic;
using System.Linq;

namespace FloorsBySketch
{
    public static class FloorsBySketch
    {
        private const string ORIGINAL_BOUNDARY_KEY = "Original Boundary";
        private const string BOUNDARY_KEY = "Boundary";
        private const string CREATION_ID_KEY = "Creation Id";
        private const double IDENTITY_CENTROID_DISTANCE_TOLERANCE = 0.01;

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
                f.AdditionalProperties[ORIGINAL_BOUNDARY_KEY] = floorInput.Boundary;
                f.AdditionalProperties[BOUNDARY_KEY] = floorInput.Boundary;
                floors.Add(f);
            }

            var overridenFloors = input.Overrides.Floors.CreateElements(
                input.Overrides.Additions.Floors,
                input.Overrides.Removals.Floors,
                (addition) => CreateFloor(addition.Id, addition.Value.Boundary, floors, floorMaterial),
                (floor, identity) => IdentityMatch(floor, identity.CreationId.ToString(), identity.OriginalBoundary.Centroid()),
                (floor, edit) => UpdateFloorBoundary(floor, edit),
                floors
            );

            overridenFloors = input.Overrides.FloorProperties.Apply(
                overridenFloors,
                (floor, identity) => IdentityMatch(floor, identity.CreationId.ToString(), identity.OriginalBoundary.Centroid()),
                (floor, edit) => UpdateThickness(floor, edit)
            );

            overridenFloors = input.Overrides.FloorElevation.Apply(
                overridenFloors,
                (floor, identity) => IdentityMatch(floor, identity.CreationId.ToString(), identity.OriginalBoundary.Centroid()),
                (floor, edit) => UpdateElevation(floor, edit)
            );

            var areaSum = floors.Sum(f => f.Profile.Area());
            var output = new FloorsBySketchOutputs(areaSum);
            var count = 1;
            foreach (var floorGrp in floors.OrderBy(f => f.Elevation).GroupBy(f => f.Elevation))
            {
                foreach (var floor in floorGrp)
                {
                    floor.Name = $"Level {count}";
                }
                count++;
            }

            output.Model.AddElements(overridenFloors);
            return output;
        }

        private static Floor CreateFloor(string additionId, Polygon additionBoundary, List<Floor> floors, Material floorMaterial)
        {
            var elevation = 0.0;
            var floorsUnderThisFloor = floors.Where(f => f.Profile.Contains(additionBoundary.Centroid()));
            if (floorsUnderThisFloor.Any())
            {
                elevation = floorsUnderThisFloor.Max(f => f.GetTopOfSlabElevation()) + DEFAULT_FLOOR_TO_FLOOR_HEIGHT;
            }

            var floor = new Floor(additionBoundary, DEFAULT_FLOOR_THICKNESS, new Transform(0, 0, elevation - DEFAULT_FLOOR_THICKNESS), floorMaterial);
            floor.AdditionalProperties[ORIGINAL_BOUNDARY_KEY] = additionBoundary;
            floor.AdditionalProperties[BOUNDARY_KEY] = additionBoundary;
            floor.AdditionalProperties[CREATION_ID_KEY] = additionId;
            floors.Add(floor);

            return floor;
        }

        private static Floor UpdateFloorBoundary(Floor floor, FloorsOverride edit)
        {
            floor.Profile = edit.Value.Boundary;
            floor.AdditionalProperties[BOUNDARY_KEY] = edit.Value.Boundary;
            return floor;
        }

        private static Floor UpdateElevation(Floor floor, FloorElevationOverride edit)
        {
            floor.Transform = edit.Value.Transform ?? floor.Transform;
            return floor;
        }

        private static Floor UpdateThickness(Floor floor, FloorPropertiesOverride edit)
        {
            floor.Thickness = edit.Value.Thickness;
            return floor;
        }

        private static bool IdentityMatch(Floor f, string identityCreationId, Vector3 identityCentroid)
        {
            f.AdditionalProperties.TryGetValue(CREATION_ID_KEY, out var creationId);
            return creationId as string == identityCreationId ||
                creationId == null && f.Profile.Perimeter.Centroid().DistanceTo(identityCentroid) < IDENTITY_CENTROID_DISTANCE_TOLERANCE;
        }
    }
}