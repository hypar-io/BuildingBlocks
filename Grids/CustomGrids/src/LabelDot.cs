using System;
using Elements.Geometry;
using Elements.Geometry.Solids;
namespace Elements
{
    public class LabelDot : GeometricElement
    {
        public string LabelText { get; set; }
        public LabelDot(Vector3 location, string text) : base(new Transform(location), BuiltInMaterials.Trans, null, false, Guid.NewGuid(), null)
        {
            this.LabelText = text;
            this.Representation = new Representation(new[] { new Lamina(Polygon.Rectangle(0.1, 0.1), false) });
        }
    }
}