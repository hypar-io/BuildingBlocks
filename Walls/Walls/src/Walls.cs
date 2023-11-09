using Elements;
using Elements.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Walls
{
    public static class Walls
    {

        private const string ADD_ID_PROPERTY_NAME = "Add Id";

        private static double DEFAULT_WALL_THICKNESS => Units.InchesToMeters(6);
        private static double DEFAULT_WALL_HEIGHT => Units.FeetToMeters(10);
        private static readonly Material DEFAULT_WALL_MATERIAL = new Material("Wall", Colors.White);
        /// <summary>
        /// The Walls function.
        /// </summary>
        /// <param name="model">The input model.</param>
        /// <param name="input">The arguments to the execution.</param>
        /// <returns>A WallsOutputs instance containing computed results and the model with any new elements.</returns>
        public static WallsOutputs Execute(Dictionary<string, Model> inputModels, WallsInputs input)
        {
            inputModels.TryGetValue("Levels", out var levelsModel);
            var levels = levelsModel?.AllElementsOfType<Level>();

            var output = new WallsOutputs();
            var walls = input.Overrides.Walls.CreateElements(
                input.Overrides.Additions.Walls,
                input.Overrides.Removals.Walls,
                (add) => CreateWall(add),
                (wall, identity) => Match(wall, identity),
                (wall, edit) => UpdateWall(wall, edit)
            );
            walls = input.Overrides.WallProperties.Apply(
                walls,
                (wall, identity) => Match(wall, identity),
                (wall, edit) => UpdateWall(wall, edit, levels));
            output.Model.AddElements(walls);
            return output;
        }

        private static StandardWall CreateWall(WallsOverrideAddition add)
        {
            var centerline = add.Value.CenterLine.Projected(Plane.XY);
            var wall = new StandardWall(centerline, DEFAULT_WALL_THICKNESS, DEFAULT_WALL_HEIGHT)
            {
                Material = DEFAULT_WALL_MATERIAL
            };
            wall.AdditionalProperties[ADD_ID_PROPERTY_NAME] = add.Id;
            Identity.AddOverrideIdentity(wall, add);
            return wall;
        }

        private static bool Match(StandardWall wall, WallsIdentity identity)
        {
            return wall.AdditionalProperties[ADD_ID_PROPERTY_NAME].ToString() == identity.AddId;
        }

        private static bool Match(StandardWall wall, WallPropertiesIdentity identity)
        {
            return wall.AdditionalProperties[ADD_ID_PROPERTY_NAME].ToString() == identity.AddId;
        }

        private static StandardWall UpdateWall(StandardWall wall, WallsOverride edit)
        {
            var centerline = edit.Value.CenterLine ?? wall.CenterLine;
            centerline = centerline.Projected(Plane.XY);
            var newWall = new StandardWall(centerline, wall.Thickness, wall.Height)
            {
                Transform = wall.Transform,
                AdditionalProperties = wall.AdditionalProperties,
                Material = wall.Material
            };
            Identity.AddOverrideIdentity(newWall, edit);
            return newWall;
        }

        private static StandardWall UpdateWall(StandardWall wall, WallPropertiesOverride edit, IEnumerable<Level> levels)
        {
            var thickness = edit.Value.Thickness ?? wall.Thickness;
            var height = edit.Value.Height ?? wall.Height;
            var centerline = wall.CenterLine;
            var addlProps = wall.AdditionalProperties;

            var transform = new Transform();

            if (levels != null && edit.Value.Levels?.BottomLevel?.Id != null)
            {
                var bottomLevel = levels.FirstOrDefault(l => l.Id.ToString() == edit.Value.Levels.BottomLevel.Id);
                if (bottomLevel == null && levels.Count() > 0)
                {
                    bottomLevel = levels.OrderBy(l => Math.Abs(l.Elevation - edit.Value.Levels.BottomLevel.Elevation.Value)).First();
                }
                if (bottomLevel != null)
                {
                    transform.Move(new Vector3(0, 0, bottomLevel.Elevation));
                }
                var topLevel = levels.FirstOrDefault(l => l.Id.ToString() == edit.Value.Levels.TopLevel.Id);
                if (edit.Value.Levels?.TopLevel?.Id != null)
                {
                    if (topLevel == null && levels.Count() > 0)
                    {
                        topLevel = levels.OrderBy(l => Math.Abs(l.Elevation - edit.Value.Levels.TopLevel.Elevation.Value)).First();
                    }
                    if (topLevel != null)
                    {
                        height = topLevel.Elevation - (bottomLevel?.Elevation ?? 0);
                    }
                }
                wall.AdditionalProperties["Levels"] = new WallPropertiesValueLevels(
                    new WallPropertiesValueBottomLevel(
                        bottomLevel?.Id.ToString(),
                        bottomLevel?.Elevation
                    ),
                    new WallPropertiesValueTopLevel(
                        topLevel?.Id.ToString(),
                        topLevel?.Elevation
                    )
                );
            }

            var newWall = new StandardWall(centerline, thickness, height)
            {
                Material = DEFAULT_WALL_MATERIAL,
                AdditionalProperties = addlProps,
                Transform = transform
            };

            if (edit.Value.Openings != null)
            {
                foreach (var openingSpec in edit.Value.Openings)
                {
                    var sillHeight = openingSpec.SillHeight ?? 0;
                    var topHeight = (openingSpec.TopOutHeight ?? Units.FeetToMeters(7.5));
                    if (topHeight - sillHeight < 0.01)
                    {
                        topHeight += Units.FeetToMeters(7.5);
                    }
                    var openingHeight = topHeight - sillHeight;
                    foreach (var line in openingSpec.Locations ?? Enumerable.Empty<Line>())
                    {
                        var pos1 = centerline.Start.DistanceTo(line.Start.ClosestPointOn(centerline));
                        var pos2 = centerline.Start.DistanceTo(line.End.ClosestPointOn(centerline));
                        if (pos2 < pos1)
                        {
                            (pos2, pos1) = (pos1, pos2);
                        }
                        var width = pos2 - pos1;

                        var pgon = Polygon.Rectangle((pos1, sillHeight), (pos2, topHeight));
                        var centerPos = pgon.Centroid();
                        newWall.AddOpening(width, openingHeight, centerPos.X, centerPos.Y);
                    }
                }
            }


            // This is only necessary because we're creating a new wall instead
            // of modifying the one that was passed in.
            Identity.AddOverrideIdentity(newWall, edit);
            return newWall;
        }
    }
}