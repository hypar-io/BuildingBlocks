using System.Collections.Generic;
using Elements.Geometry;
using Elements.Geometry.Solids;

namespace Elements
{
    public partial class Roof
    {
        public static Material RoofMaterial { get; set; } = new Material("Roof", Colors.Gray, 0.1, 0.1, null);
        public static Material InstallationMaterial { get; set; } = new Material("Installation", Colors.Yellow, 0.1, 0.1, null);
        public Roof(Profile p, double thickness, Transform t, bool isInstallation = false) : this(p, p.Area(), t, isInstallation? InstallationMaterial: RoofMaterial , new Representation(new List<SolidOperation>()))
        {
            this.Representation = new Representation(new[] { new Extrude(p, thickness, Vector3.ZAxis, false) });
        }
    }
}