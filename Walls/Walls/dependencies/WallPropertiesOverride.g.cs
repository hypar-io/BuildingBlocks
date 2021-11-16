using Elements;
using System.Collections.Generic;

namespace Walls
{
	/// <summary>
	/// Override metadata for WallPropertiesOverride
	/// </summary>
	public partial class WallPropertiesOverride : IOverride
	{
        public static string Name = "Wall Properties";
        public static string Dependency = null;
        public static string Context = "[*discriminator=Elements.StandardWall]";
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