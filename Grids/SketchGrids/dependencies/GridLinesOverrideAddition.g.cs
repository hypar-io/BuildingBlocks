using Elements;
using System.Collections.Generic;
using System;
using System.Linq;

namespace SketchGrids
{
	/// <summary>
	/// Override metadata for GridLinesOverrideAddition
	/// </summary>
	public partial class GridLinesOverrideAddition : IOverride
	{
        public static string Name = "Grid Lines Addition";
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