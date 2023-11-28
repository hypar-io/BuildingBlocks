using Elements;

namespace FloorsBySketch
{
    public static class FloorHelpers
    {
        public static double GetTopOfSlabElevation(this Floor f)
        {
            return f.Transform.Origin.Z + f.Thickness;
        }
    }
}