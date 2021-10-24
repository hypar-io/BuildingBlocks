using Elements;
using Elements.Geometry;
using System.Collections.Generic;
using System.Linq;

namespace Walls
{
    public static class Walls
    {
        /// <summary>
        /// The Walls function.
        /// </summary>
        /// <param name="model">The input model.</param>
        /// <param name="input">The arguments to the execution.</param>
        /// <returns>A WallsOutputs instance containing computed results and the model with any new elements.</returns>
        public static WallsOutputs Execute(Dictionary<string, Model> inputModels, WallsInputs input)
        {
            var output = new WallsOutputs();
            var defaultWallMaterial = new Material("Wall", Colors.White);
            if (input.Overrides?.Additions?.Walls != null)
            {
                // Get identities for additions
                var additionCenters = input.Overrides.Additions.Walls.Select(x => (x.Value.CenterLine.PointAt(0.5), x.Id)).Cast<(Vector3 RoughLocation, string Id)>().ToList();

                // match edit overrides to addition Ids.
                var editsByAdditionId = input.Overrides.Walls?.Select(wallEdit =>
                    (additionCenters.OrderBy(ac => ac.RoughLocation.DistanceTo(wallEdit.Identity.RoughLocation)).First().Id, wallEdit)
                  ).ToDictionary(x => x.Id, x => x.wallEdit) ?? new Dictionary<string, WallsOverride>();

                // match property edit overrides to addition Ids. We have to do a little extra manual cleanup here,
                // since the property edits are a separate override altogether — the hypar web UI doesn't
                // automatically remove these from the overrides list.
                var propertiesByAdditionId = new Dictionary<string, WallPropertiesOverride>();
                foreach (var match in input.Overrides.WallProperties?.Select(wallEdit =>
                   (additionCenters.OrderBy(ac => ac.RoughLocation.DistanceTo(wallEdit.Identity.RoughLocation)).First().Id, wallEdit)
                ) ?? new List<(string Id, WallPropertiesOverride)>())
                {
                    if (!propertiesByAdditionId.ContainsKey(match.Id))
                    {
                        propertiesByAdditionId.Add(match.Id, match.wallEdit);
                    }
                    else // non-associated edits don't get deleted automatically, so we might have strays. Find out which one is closer.
                    {
                        var currentEdit = propertiesByAdditionId[match.Id];
                        var additionCenter = additionCenters.First(ac => ac.Id == match.Id);
                        if (currentEdit.Identity.RoughLocation.DistanceTo(additionCenter.RoughLocation) > match.wallEdit.Identity.RoughLocation.DistanceTo(additionCenter.RoughLocation))
                        {
                            propertiesByAdditionId[match.Id] = match.wallEdit;
                        }
                    }
                }

                // for every addition
                foreach (var newWall in input.Overrides.Additions.Walls)
                {
                    var wallLine = newWall.Value.CenterLine;
                    var wallCenter = wallLine.PointAt(0.5);

                    // get matching edit overrides
                    editsByAdditionId.TryGetValue(newWall.Id, out var matchingEdits);
                    wallLine = matchingEdits?.Value?.CenterLine ?? wallLine;

                    propertiesByAdditionId.TryGetValue(newWall.Id, out var matchingProperties);
                    var wallThickness = matchingProperties?.Value.Thickness ?? 0.15;
                    var wallheight = matchingProperties?.Value.Height ?? 3.0;

                    // create wall
                    var wall = new StandardWall(wallLine, wallThickness, wallheight, defaultWallMaterial);

                    // attach identity information and associated overrides
                    wall.AdditionalProperties["Rough Location"] = wallCenter;
                    Identity.AddOverrideIdentity(wall, newWall);
                    if (matchingProperties != null)
                    {
                        Identity.AddOverrideIdentity(wall, matchingProperties);
                    }
                    if (matchingEdits != null)
                    {
                        Identity.AddOverrideIdentity(wall, matchingEdits);
                    }

                    // add wall to model
                    output.Model.AddElement(wall);
                }
            }
            return output;
        }
    }
}