using Polygon = Elements.Geometry.Polygon;
namespace Elements
{
    // Adding on to partial class to add new property just for this function 
    // (As an alternative to using AdditionalProperties)

    public partial class Grid2dElement : GeometricElement
    {
        public Polygon Extents { get; set; }
    }
}