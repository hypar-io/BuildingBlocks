using Elements;
using System.Collections.Generic;

namespace FacadeStrategy2
{
	/// <summary>
	/// Override metadata for FacadeStrategy2SettingsOverride
	/// </summary>
	public partial class FacadeStrategy2SettingsOverride : IOverride
	{
        public static string Name = "Facade Strategy 2 Settings";
        public static string Dependency = "Facade Grid";
        public static string Context = "[*discriminator=Elements.FacadeType]";
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
		public static IEnumerable<ElementProxy<Elements.FacadeType>> ContextProxies(Dictionary<string, Model> models) {
			return models.AllElementsOfType<Elements.FacadeType>(Dependency).Proxies(Dependency);
		}
	}
}