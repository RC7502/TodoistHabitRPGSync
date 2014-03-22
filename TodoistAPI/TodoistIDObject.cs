using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace Deap.Todoist.Api
{
	/// <summary>
	/// Core base class for all todoist items that has an ID.
	/// </summary>
	[DataContract]
	public abstract class TodoistIDObject : TodoistObject
	{
		/// <summary>
		/// Gets the id of the object.
		/// </summary>
		[DataMember(Name = "id")]
		public long ID { get; internal set; }

	}
}