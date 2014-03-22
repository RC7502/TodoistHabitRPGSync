using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace Deap.Todoist.Api
{
	/// <summary>
	/// The core base class for all todoist items.
	/// </summary>
	[DataContract]
	public abstract class TodoistObject
	{
		/// <summary>
		/// A reference back to the users object this todoist item belongs to.
		/// </summary>
		public User Owner { get; internal set; }

		/// <summary>
		/// This method is called right after a json responce has been deserialized to a strongly typed object. Place any kind of initialization code here, such as assigning the Owner member.
		/// </summary>
		/// <param name="todoistUser">The current todoist user.</param>
		public virtual void Initialize(User todoistUser)
		{
			this.Owner = todoistUser;
		}
	}
}
