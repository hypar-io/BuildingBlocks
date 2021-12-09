using Elements.Geometry;

namespace Grid
{
    public class GridGuide
    {
        public Vector3 Point;
        public string Name;

        public GridGuide(Vector3 point, string name = null)
        {
            this.Point = point;
            this.Name = name;
        }
    }
}