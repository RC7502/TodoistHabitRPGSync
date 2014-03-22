using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace Deap.Todoist.Api
{
	[DataContract]
	public class QueryResult : TodoistObject
	{
		[DataMember(Name = "type")]
		public string Type { get; internal set; }

		[DataMember(Name = "query")]
		public string Query { get; internal set; }

		[DataMember(Name = "data")]
		public Item[] Items { get; internal set; }

		public override void Initialize(User todoistUser)
		{
			base.Initialize(todoistUser);
			foreach (Item item in this.Items)
			{
				item.Initialize(todoistUser);
			}
		}
	}
}