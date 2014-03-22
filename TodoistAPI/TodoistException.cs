using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Deap.Todoist.Api
{
	public class TodoistException : Exception
	{
		public TodoistException(string message, Exception innerException) : base(message, innerException)
		{
		}
	}
}
