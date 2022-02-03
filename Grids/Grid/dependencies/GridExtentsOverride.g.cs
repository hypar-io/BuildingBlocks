using Elements;
using System.Collections.Generic;

namespace Grid
{
	/// <summary>
	/// Override metadata for GridExtentsOverride
	/// </summary>
	public partial class GridExtentsOverride : IOverride
	{
        public static string Name = "Grid Extents";
        public static string Dependency = null;
        public static string Context = "[*discriminator=Elements.Grid2dElement]";
		public static string Paradigm = "Edit";

        /// <summary>
        /// Get the override name for this override.
        /// </summary>
        public string GetName() {
			return Name;
		}

		public object GetIdentity() {

			return Identity;
		}

	}
}