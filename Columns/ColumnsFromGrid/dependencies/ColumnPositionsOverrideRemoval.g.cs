using Elements;
using System.Collections.Generic;
using System;
using System.Linq;

namespace ColumnsFromGrid
{
	/// <summary>
	/// Override metadata for ColumnPositionsOverrideRemoval
	/// </summary>
	public partial class ColumnPositionsOverrideRemoval : IOverride
	{
        public static string Name = "Column Positions Removal";
        public static string Dependency = null;
        public static string Context = "[*discriminator=Elements.Column]";
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