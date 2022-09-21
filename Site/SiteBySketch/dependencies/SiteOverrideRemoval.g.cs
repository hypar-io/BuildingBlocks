using Elements;
using System.Collections.Generic;
using System;
using System.Linq;

namespace SiteBySketch
{
	/// <summary>
	/// Override metadata for SiteOverrideRemoval
	/// </summary>
	public partial class SiteOverrideRemoval : IOverride
	{
        public static string Name = "Site Removal";
        public static string Dependency = null;
        public static string Context = "[*discriminator=Elements.Site]";
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