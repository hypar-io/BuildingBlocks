using Elements.Geometry;
using Elements.Geometry.Solids;

namespace Elements
{
    public class FacadePanel : Panel
    {
        public FacadePanel(Polygon perimeter) : base(perimeter)
        {

        }
        public double Thickness { get; set; } = 0;
        public override void UpdateRepresentations()
        {
            if (Thickness == 0)
            {
                base.UpdateRepresentations();
            }
            else
            {
                Representation = new Extrude(Perimeter, Thickness, Vector3.ZAxis, false);
            }
        }
    }
}