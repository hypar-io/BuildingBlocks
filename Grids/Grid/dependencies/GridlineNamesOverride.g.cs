using Elements;
using System.Collections.Generic;

namespace Grid
{
	/// <summary>
	/// Override metadata for GridlineNamesOverride
	/// </summary>
	public partial class GridlineNamesOverride : IOverride
	{
        public static string Name = "Gridline Names";
        public static string Dependency = null;
        public static string Context = "[*discriminator=Elements.GridLine]";
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