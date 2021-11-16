using Elements;
using System.Collections.Generic;

namespace Walls
{
	/// <summary>
	/// Override metadata for WallsOverride
	/// </summary>
	public partial class WallsOverride : IOverride
	{
        public static string Name = "Walls";
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