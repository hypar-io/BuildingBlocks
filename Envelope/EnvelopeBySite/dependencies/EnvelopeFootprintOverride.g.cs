using Elements;
using System.Collections.Generic;
using System;
using System.Linq;

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
	public static class EnvelopeFootprintOverrideExtensions
    {
		/// <summary>
        /// Apply Envelope Footprint edit overrides to a collection of existing elements
        /// </summary>
        /// <param name="overrideData">The Envelope Footprint Overrides to apply</param>
        /// <param name="existingElements">A collection of existing elements to which to apply the overrides.</param>
        /// <param name="identityMatch">A function returning a boolean which indicates whether an element is a match for an override's identity.</param>
        /// <param name="modifyElement">A function to modify a matched element, returning the modified element.</param>
        /// <typeparam name="T">The element type this override applies to. Should match the type(s) in the override's context.</typeparam>
        /// <returns>A collection of elements, including unmodified and modified elements from the supplied collection.</returns>
        public static List<T> Apply<T>(
            this IList<EnvelopeFootprintOverride> overrideData,
            IEnumerable<T> existingElements,
            Func<T, EnvelopeFootprintIdentity, bool> identityMatch,
            Func<T, EnvelopeFootprintOverride, T> modifyElement) where T : Element
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
        /// Apply Envelope Footprint edit overrides to a collection of existing elements
        /// </summary>
        /// <param name="existingElements">A collection of existing elements to which to apply the overrides.</param>
        /// <param name="overrideData">The Envelope Footprint Overrides to apply — typically `input.Overrides.EnvelopeFootprint`</param>
        /// <param name="identityMatch">A function returning a boolean which indicates whether an element is a match for an override's identity.</param>
        /// <param name="modifyElement">A function to modify a matched element, returning the modified element.</param>
        /// <typeparam name="T">The element type this override applies to. Should match the type(s) in the override's context.</typeparam>
        /// <returns>A collection of elements, including unmodified and modified elements from the supplied collection.</returns>
        public static void ApplyOverrides<T>(
            this List<T> existingElements,
            IList<EnvelopeFootprintOverride> overrideData,
            Func<T, EnvelopeFootprintIdentity, bool> identityMatch,
            Func<T, EnvelopeFootprintOverride, T> modifyElement
            ) where T : Element
        {
            var updatedElements = overrideData.Apply(existingElements, identityMatch, modifyElement);
            existingElements.Clear();
            existingElements.AddRange(updatedElements);
        }
		
	}


}