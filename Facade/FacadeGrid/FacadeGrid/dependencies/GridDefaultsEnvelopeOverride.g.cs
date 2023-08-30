using Elements;
using System.Collections.Generic;

namespace FacadeGrid
{
	/// <summary>
	/// Override metadata for GridDefaultsEnvelopeOverride
	/// </summary>
	public partial class GridDefaultsEnvelopeOverride : IOverride
	{
        public static string Name = "Grid Defaults - Envelope";
        public static string Dependency = "Envelope";
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

		/// <summary>
		/// Get context elements that are applicable to this override.
		/// </summary>
		/// <param name="models">Dictionary of input models, or any other kind of dictionary of models.</param>
		/// <returns>List of context elements that match what is defined on the override.</returns>
		public static IEnumerable<ElementProxy<Elements.Envelope>> ContextProxies(Dictionary<string, Model> models) {
			return models.AllElementsOfType<Elements.Envelope>(Dependency).Proxies(Dependency);
		}
	}
}