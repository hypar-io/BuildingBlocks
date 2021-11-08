using Elements;
using System.Collections.Generic;

namespace Grid
{
	/// <summary>
	/// Override metadata for UGridSubdivisionsOverride
	/// </summary>
	public partial class UGridSubdivisionsOverride : IOverride
	{
        public static string Name = "U Grid Subdivisions";
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