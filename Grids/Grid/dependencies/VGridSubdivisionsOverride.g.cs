using Elements;
using System.Collections.Generic;

namespace Grid
{
	/// <summary>
	/// Override metadata for VGridSubdivisionsOverride
	/// </summary>
	public partial class VGridSubdivisionsOverride : IOverride
	{
        public static string Name = "V Grid Subdivisions";
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