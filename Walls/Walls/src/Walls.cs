using DotLiquid;
using Elements;
using Elements.Geometry;
using System.Collections.Generic;

namespace Walls
{
  public static class Walls
  {
    private static Material WallMaterial = new Material("Wall", Colors.White, 0.0f, 0.0f);
    /// <summary>
    /// The Walls function.
    /// </summary>
    /// <param name="model">The input model.</param>
    /// <param name="input">The arguments to the execution.</param>
    /// <returns>A WallsOutputs instance containing computed results and the model with any new elements.</returns>
    public static WallsOutputs Execute(Dictionary<string, Model> inputModels, WallsInputs input)
    {

      var levelVolumes = new List<LevelVolume>();
      if (inputModels.TryGetValue("Levels", out var levelsModel))
      {
        levelVolumes.AddRange(levelsModel.AllElementsOfType<LevelVolume>());
      }
      if (inputModels.TryGetValue("Conceptual Mass", out var massModel))
      {
        levelVolumes.AddRange(massModel.AllElementsOfType<LevelVolume>());
      }


      var output = new WallsOutputs();
      var protoSegments = input.Overrides.Walls.CreateElements(
        input.Overrides.Additions.Walls,
        input.Overrides.Removals.Walls,
        (add) => new ProtoSegment(add, levelVolumes),
        (wall, identity) => identity.AddId == wall.AddId,
        (wall, edit) => wall.Update(edit)
      );
      var protoSegmentsByLevel = protoSegments.GroupBy(ps => ps.LevelVolume?.AddId ?? "No Level");
      foreach (var segmentGroup in protoSegmentsByLevel)
      {
        var thickenedPolylines = segmentGroup.Select(ps => ps.Geometry);
        var offsetGeometry = IThickenedPolyline.GetPolygons(thickenedPolylines);
        if (thickenedPolylines.Count() != offsetGeometry.Count)
        {
          output.Warnings.Add("Something strange happened with offset geometry. Try undoing your change and trying again.");
          continue;
        }
        for (int i = 0; i < segmentGroup.Count(); i++)
        {
          var segment = segmentGroup.ElementAt(i);
          var offset = offsetGeometry[i];
          if (offset.offsetPolygon == null)
          {
            continue;
          }
          var wall = new Wall(offset.offsetPolygon, segment.Height, WallMaterial, segment.LevelVolume?.Transform ?? new Transform());
          wall.AdditionalProperties["Geometry"] = segment.Geometry;
          wall.AdditionalProperties["Add Id"] = segment.AddId;
          wall.AdditionalProperties["Level"] = segment.LevelVolume?.Id;

          output.Model.AddElement(wall);
        }
      }
      return output;
    }
  }
}