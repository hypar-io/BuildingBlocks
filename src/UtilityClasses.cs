using Elements.Geometry;

namespace CustomGrids
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

    public class GridLine
    {
        public Line Line;
        public string Name;

        public GridLine(Line line, string name = null)
        {
            this.Line = line;
            this.Name = name;
        }
    }
}