using Elements;
using System.Collections.Generic;
using System;
using System.Linq;

namespace SketchGrids
{
	/// <summary>
	/// Override metadata for GridLinesOverrideRemoval
	/// </summary>
	public partial class GridLinesOverrideRemoval : IOverride
	{
        public static string Name = "Grid Lines Removal";
        public static string Dependency = null;
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

	}

}