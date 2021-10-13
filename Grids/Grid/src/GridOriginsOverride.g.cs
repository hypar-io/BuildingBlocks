using Elements;
using System.Collections.Generic;

namespace Grid
{
	/// <summary>
	/// Override metadata for GridOriginsOverride
	/// </summary>
	public partial class GridOriginsOverride
	{
        public static string Name = "Grid Origins";
        public static string Dependency = null;
        public static string Context = "[*discriminator=Elements.Grid2dElement]";

	}
}