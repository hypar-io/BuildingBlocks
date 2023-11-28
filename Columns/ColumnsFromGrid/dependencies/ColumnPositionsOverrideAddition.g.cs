using Elements;
using System.Collections.Generic;
using System;
using System.Linq;

namespace ColumnsFromGrid
{
	/// <summary>
	/// Override metadata for ColumnPositionsOverrideAddition
	/// </summary>
	public partial class ColumnPositionsOverrideAddition : IOverride
	{
        public static string Name = "Column Positions Addition";
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