using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Deap.Todoist.Api
{
	public class TimeZoneOffset
	{
		public string GmtString { get; set; }
		public int Hours { get; set; }
		public int Minutes { get; set; }
		public bool IsDaylightSavingsTime { get; set; }

		internal TimeZoneOffset(string gmtString, int hours, int minutes, bool isDaylightSavingsTime)
		{
			GmtString = gmtString;
			Hours = hours;
			Minutes = minutes;
			IsDaylightSavingsTime = isDaylightSavingsTime;
		}
	}
}
