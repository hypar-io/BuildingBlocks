using Elements;
using Elements.Geometry;

namespace EmergencyEgress
{
    public static class ThickenedPolylineExtensions
    {
        public static double GetWidth(this ThickenedPolyline polyline)
        {
            // if (polyline.Width.HasValue)
            // {
            //     return polyline.Width.Value;
            // }
            return polyline.LeftWidth + polyline.RightWidth;
        }

        public static double GetOffset(this ThickenedPolyline polyline)
        {
            double offsetDistance = 0;
            //Old way of storing data inside CirculationSegment.
            // if (polyline.Width.HasValue && polyline.Flip.HasValue)
            // {
            //     offsetDistance = polyline.Width.Value / 2;
            //     if (polyline.Flip.Value)
            //     {
            //         offsetDistance = -offsetDistance;
            //     }
            // }
            // else
            // {
            offsetDistance = (polyline.RightWidth - polyline.LeftWidth) / 2;
            // }
            return offsetDistance;
        }
    }
}
