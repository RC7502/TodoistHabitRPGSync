using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Runtime.Serialization;

namespace Deap.Todoist.Api
{
	public enum ReminderType
	{
		NoDefault,
		Email,
		Mobile,
		Twitter
	}
}
