using Elements;
using System.Collections.Generic;
using System;
using System.Linq;

namespace ColumnsFromGrid
{
	/// <summary>
	/// Override metadata for GridlinesOptionsOverride
	/// </summary>
	public partial class GridlinesOptionsOverride : IOverride
	{
        public static string Name = "Gridlines Options";
        public static string Dependency = "Grids";
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

		/// <summary>
		/// Get context elements that are applicable to this override.
		/// </summary>
		/// <param name="models">Dictionary of input models, or any other kind of dictionary of models.</param>
		/// <returns>List of context elements that match what is defined on the override.</returns>
		public static IEnumerable<ElementProxy<Elements.GridLine>> ContextProxies(Dictionary<string, Model> models) {
			return models.AllElementsOfType<Elements.GridLine>(Dependency).Proxies(Dependency);
		}
	}
	public static class GridlinesOptionsOverrideExtensions
    {
		/// <summary>
        /// Apply Gridlines Options edit overrides to a collection of existing elements
        /// </summary>
        /// <param name="overrideData">The Gridlines Options Overrides to apply</param>
        /// <param name="existingElements">A collection of existing elements to which to apply the overrides.</param>
        /// <param name="identityMatch">A function returning a boolean which indicates whether an element is a match for an override's identity.</param>
        /// <param name="modifyElement">A function to modify a matched element, returning the modified element.</param>
        /// <typeparam name="T">The element type this override applies to. Should match the type(s) in the override's context.</typeparam>
        /// <returns>A collection of elements, including unmodified and modified elements from the supplied collection.</returns>
        public static List<T> Apply<T>(
            this IList<GridlinesOptionsOverride> overrideData,
            IEnumerable<T> existingElements,
            Func<T, GridlinesOptionsIdentity, bool> identityMatch,
            Func<T, GridlinesOptionsOverride, T> modifyElement) where T : Element
        {
            var resultElements = new List<T>(existingElements);
            if (overrideData != null)
            {
                foreach (var overrideValue in overrideData)
                {
                    // Assuming there will only be one match per identity, find the first element that matches.
                    var matchingElement = existingElements.FirstOrDefault(e => identityMatch(e, overrideValue.Identity));
                    // if we found a match,
                    if (matchingElement != null)
                    {
                        // remove the old matching element
                        resultElements.Remove(matchingElement);
                        // apply the modification function to it 
                        var modifiedElement = modifyElement(matchingElement, overrideValue);
                        // set the identity
                        Identity.AddOverrideIdentity(modifiedElement, overrideValue);
                        //and re-add it to the collection
                        resultElements.Add(modifiedElement);
                    }
                }
            }
            return resultElements;
        }

		/// <summary>
        /// Apply Gridlines Options edit overrides to a collection of existing elements
        /// </summary>
        /// <param name="existingElements">A collection of existing elements to which to apply the overrides.</param>
        /// <param name="overrideData">The Gridlines Options Overrides to apply — typically `input.Overrides.GridlinesOptions`</param>
        /// <param name="identityMatch">A function returning a boolean which indicates whether an element is a match for an override's identity.</param>
        /// <param name="modifyElement">A function to modify a matched element, returning the modified element.</param>
        /// <typeparam name="T">The element type this override applies to. Should match the type(s) in the override's context.</typeparam>
        /// <returns>A collection of elements, including unmodified and modified elements from the supplied collection.</returns>
        public static void ApplyOverrides<T>(
            this List<T> existingElements,
            IList<GridlinesOptionsOverride> overrideData,
            Func<T, GridlinesOptionsIdentity, bool> identityMatch,
            Func<T, GridlinesOptionsOverride, T> modifyElement
            ) where T : Element
        {
            var updatedElements = overrideData.Apply(existingElements, identityMatch, modifyElement);
            existingElements.Clear();
            existingElements.AddRange(updatedElements);
        }
		
	}


}