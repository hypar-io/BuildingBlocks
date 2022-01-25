using Elements;
using System.Collections.Generic;

namespace Grid
{
	/// <summary>
	/// Override metadata for GridOriginsOverride
	/// </summary>
	public partial class GridOriginsOverride : IOverride
	{
        public static string Name = "Grid Origins";
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