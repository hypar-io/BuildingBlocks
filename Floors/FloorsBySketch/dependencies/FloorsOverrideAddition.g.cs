using Elements;
using System.Collections.Generic;
using System;
using System.Linq;

namespace FloorsBySketch
{
	/// <summary>
	/// Override metadata for FloorsOverrideAddition
	/// </summary>
	public partial class FloorsOverrideAddition : IOverride
	{
        public static string Name = "Floors Addition";
        public static string Dependency = null;
        public static string Context = "[*discriminator=Elements.Floor]";
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