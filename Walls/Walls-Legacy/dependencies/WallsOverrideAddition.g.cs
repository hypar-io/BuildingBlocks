using Elements;
using System.Collections.Generic;
using System;
using System.Linq;

namespace Walls
{
	/// <summary>
	/// Override metadata for WallsOverrideAddition
	/// </summary>
	public partial class WallsOverrideAddition : IOverride
	{
        public static string Name = "Walls Addition";
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