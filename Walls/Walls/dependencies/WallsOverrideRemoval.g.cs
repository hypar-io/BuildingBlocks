using Elements;
using System.Collections.Generic;
using System;
using System.Linq;

namespace Walls
{
	/// <summary>
	/// Override metadata for WallsOverrideRemoval
	/// </summary>
	public partial class WallsOverrideRemoval : IOverride
	{
        public static string Name = "Walls Removal";
        public static string Dependency = null;
        public static string Context = "[*discriminator=Elements.Wall]";
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