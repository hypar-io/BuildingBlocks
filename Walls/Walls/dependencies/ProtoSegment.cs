using Walls;
using Elements.Geometry;

namespace Elements
{
    public class ProtoSegment : Element
    {

        public ProtoSegment(WallsOverrideAddition add, IEnumerable<LevelVolume> levelVolumes)
        {
            Geometry = add.Value.Geometry;
            Geometry.Polyline = Geometry.Polyline.Project(Plane.XY);
            AddId = add.Id;
            var matchingLevelVolume = levelVolumes.FirstOrDefault(lv => lv.AddId != null && lv.AddId == add.Value.Level?.AddId) ??
                levelVolumes.FirstOrDefault(lv => lv.Name == add.Value.Level.Name && lv.BuildingName == add.Value.Level.BuildingName) ??
                levelVolumes.FirstOrDefault(lv => lv.Name == add.Value.Level.Name);
            LevelVolume = matchingLevelVolume;
            if (add.Value.Height <= 0)
            {
                Height = matchingLevelVolume?.Height ?? 3;
            }
            else
            {
                Height = add.Value.Height;
            }
        }

        public LevelVolume? LevelVolume { get; init; }
        public ThickenedPolyline Geometry { get; set; }

        public double Height { get; set; }

        public string AddId { get; init; }

        public bool Match(WallsIdentity identity)
        {
            return AddId == identity.AddId;
        }

        public ProtoSegment Update(WallsOverride edit)
        {
            this.Geometry = edit.Value.Geometry ?? this.Geometry;
            this.Geometry.Polyline = this.Geometry.Polyline.Project(Plane.XY);
            if (edit.Value.Height > 0)
            {
                this.Height = edit.Value.Height;
            }
            return this;
        }
    }
}