using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Drawing;
using System.Web;

namespace Deap.Todoist.Api
{
	[DataContract]
	public class Project : TodoistIDObject
	{
		//Commands
		private const string CMD_GetUncompletedItems = "getUncompletedItems";
		private const string CMD_GetCompletedItems = "getCompletedItems";


		#region Properties and API mappings

		[DataMember(Name = "user_id")]
		public int UserID { get; set; }

		[DataMember(Name = "name")]
		public string Name { get; set; }

		[DataMember(Name = "cache_count")]
		public string ItemCount { get; set; }

		[DataMember(Name = "color")]
		public string ColorHexadecimal { get; set; }

		public Color Color
		{
			get
			{
				return Color.FromArgb(Int32.Parse(this.ColorHexadecimal, System.Globalization.NumberStyles.HexNumber));
			}
			set
			{
				this.ColorHexadecimal = value.ToArgb().ToString("X");
			}
		}

		/// <summary>
		/// Indentation level of the project. Ranges from 1 to 4 (1 is top level).
		/// </summary>
		[DataMember(Name = "indent")]
		public int Indent { get; set; } 

		#endregion
	

		#region Project Hierarchy

		protected List<Project> _projects;

		/// <summary>
		/// The child projects of the current project.
		/// </summary>
		public IEnumerable<Project> Projects
		{
			get
			{
				EnsureProjects();
				return this._projects;
			}
		}

		/// <summary>
		/// The parent project to the current project. Null if this is a root project.
		/// </summary>
		public Project Parent { get; set; }

		//Used to build project hierarchy.
		internal void AddChildProject(Project project)
		{
			project.Parent = this;

			EnsureProjects();
			_projects.Add(project);
		}

		private void EnsureProjects()
		{
			if (_projects == null)
			{
				_projects = new List<Project>();
			}
		}

		#endregion


		#region Get Items

		/// <summary>
		/// Returns all uncompleted items in this project.
		/// </summary>
		public Item[] GetUncompletedItems()
		{
			bool recursive = false;
			return GetUncompletedItems(recursive);
		}

		/// <summary>
		/// Gets all uncompleted items in this project, and completed items sub-leveled to an item that is not completed.
		/// </summary>
		/// <param name="recursive">True to include uncompleted items in sub-leveled projects. Defaults to false.</param>
		public Item[] GetUncompletedItems(bool recursive)
		{
			return GetItems(CMD_GetUncompletedItems, false, recursive);
		}

		/// <summary>
		/// Gets all uncompleted root items in this project.
		/// </summary>
		public Item[] GetUncompletedRootItems()
		{
			return GetItems(CMD_GetUncompletedItems, true, false);
		}

		/// <summary>
		/// Gets all completed items in this project, except those that are sub-leveled to an item that is not completed.
		/// </summary>
		public Item[] GetCompletedItems()
		{
			bool recursive = false;
			return GetCompletedItems(recursive);
		}

		/// <summary>
		/// Gets all completed items in this project, except those that are sub-leveled to an item that is not completed.
		/// </summary>
		/// <param name="recursive">True to include uncompleted items in sub-leveled projects. Defaults to false.</param>
        public Item[] GetCompletedItems(bool recursive)
		{
			return GetItems(CMD_GetCompletedItems, false, recursive);
		}

		/// <summary>
		/// Gets all completed root items in this project.
		/// </summary>
		public Item[] GetCompletedRootItems()
		{
			return GetItems(CMD_GetCompletedItems, true, false);
		}

		/// <summary>
		/// Gets all (completed and uncompleted) items in this project.
		/// </summary>
		public Item[] GetItems()
		{
			bool recursive = false;
			return GetItems(recursive);
		}

		/// <summary>
		/// Gets all (completed and uncompleted) items in this project.
		/// </summary>
		/// <param name="recursive">True to include items in sub-leveled projects. Defaults to false.</param>
		public Item[] GetItems(bool recursive)
		{
			List<Item> result = new List<Item>();

			result.AddRange(GetUncompletedItems(recursive));
			result.AddRange(GetCompletedItems(recursive));

			//IDEA: Sort items before returning? If so, sort on what?

			return result.ToArray();
		}

		/// <summary>
		/// Gets all (completed and uncompleted) root items in this project.
		/// </summary>
		public Item[] GetRootItems()
		{
			List<Item> result = new List<Item>();

			result.AddRange(GetUncompletedRootItems());
			result.AddRange(GetCompletedRootItems());

			//IDEA: Sort items before returning? If so, sort on what?

			return result.ToArray();
		}

		private Item[] GetItems(string command, bool rootItemsOnly, bool recursive)
		{
			if (!(command == CMD_GetUncompletedItems || command == CMD_GetCompletedItems))
			{
				throw new ArgumentException(String.Format("command has to be either {0} or {1}", CMD_GetUncompletedItems, CMD_GetCompletedItems), command);
			}

			List<Item> result = new List<Item>();
			Item[] items = Todoist.Request<Item[]>(command, "project_id=" + this.ID, this.Owner);
			if (items != null)
			{
				result.AddRange(items);
			}

			BuildItemHierarchy(items);

			if (rootItemsOnly)
			{
				result = (from i in items where i.Indent == 1 select i).ToList();
			}
			else if (recursive)
			{
				foreach (Project p in this.Projects)
				{
					Item[] ui = p.GetItems(command, false, true);
					if (ui != null)
					{
						result.AddRange(ui);
					}
				}
			}

			return result.ToArray();
		}

		private void BuildItemHierarchy(Item[] items)
		{
			List<Item> rootItems = new List<Item>();
			Item previousItem = null;
			foreach (Item item in items)
			{
				if (item.Indent > 1 && previousItem != null)
				{
					if (item.Indent > previousItem.Indent)
					{
						previousItem.AddChildItem(item);
					}
					else if (item.Indent == previousItem.Indent)
					{
						previousItem.Parent.AddChildItem(item);
					}
					else if (item.Indent < previousItem.Indent)
					{
						Item previousSibling = previousItem.Parent;
						while (item.Indent != previousSibling.Indent)
						{
							previousSibling = previousSibling.Parent;
						}
						previousSibling.Parent.AddChildItem(item);
					}
				}
				previousItem = item;
			}
		}
		
		#endregion


		/// <summary>
		/// Adds an item to this Project.
		/// </summary>
		/// <param name="item">The item to add. Properties that can be set are Content, DateString and Priority.</param>
		/// <returns>The added item.</returns>
		public Item AddItem(Item item)
		{
			string dateParam = string.Empty;
			if(!String.IsNullOrEmpty(item.DateString))
			{
				dateParam = "&date_string=" + item.DateString;
			}

			return Todoist.Request<Item>("addItem", String.Format("project_id={0}&content={1}&priority={2}{3}", this.ID, HttpUtility.UrlEncode(item.Content), item.Priority, dateParam), this.Owner);
		}
	}
}