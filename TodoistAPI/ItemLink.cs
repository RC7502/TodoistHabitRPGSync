using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Deap.Todoist.Api
{
	/// <summary>
	/// Represents a link in a todoist item.
	/// </summary>
	public class ItemLink
	{
		/// <summary>
		/// The URL of the link.
		/// </summary>
		public string Url { get; set; }

		/// <summary>
		/// The description of the link.
		/// </summary>
		public string Description { get; set; }
	}
}
