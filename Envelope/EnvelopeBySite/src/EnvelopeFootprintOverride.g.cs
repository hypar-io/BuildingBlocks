using Elements;
using System.Collections.Generic;

namespace EnvelopeBySite
{
	/// <summary>
	/// Override metadata for EnvelopeFootprintOverride
	/// </summary>
	public partial class EnvelopeFootprintOverride : IOverride
	{
        public static string Name = "Envelope Footprint";
        public static string Dependency = null;
        public static string Context = "[*discriminator=Elements.Envelope]";
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