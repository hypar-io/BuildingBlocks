using Elements.Geometry;

namespace Grid
{
    public class GridGuide
    {
        public Vector3 Point;
        public double ParametrizedPosition;
        public string Name;

        public GridGuide(Vector3 point, double parametrizedPosition, string name = null)
        {
            this.Point = point;
            this.ParametrizedPosition = parametrizedPosition;
            this.Name = name;
        }
    }
}