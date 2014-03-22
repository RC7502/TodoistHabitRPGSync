using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Deap.Todoist.Api
{
	[DataContract]
	public class Item : TodoistIDObject
	{
		#region Properties and API mappings
		
		/// <summary>
		/// The DueDate of the item, if any. To display the DueDate, you might wanna use the ShowDueDate property instead.
		/// </summary>
		[DataMember(Name = "due_date")]
		protected internal string _dueDate { get; set; }
		public DateTime? DueDate
		{
		    get
			{
				DateTime result;
				if (DateTime.TryParse(_dueDate, out result))
				{
					return result;
				}
				return null;
			}
		    set
		    {
		        if (value.HasValue)
		            _dueDate = value.Value.ToShortDateString();
		    }
		}

	    [DataMember(Name = "user_id")]
		public int UserID { get; set; }

		[DataMember(Name = "collapsed")]
		public bool Collapsed { get; set; }

		[DataMember(Name = "in_history")]
		public bool InHistory { get; set; }

		private int _priority;
		/// <summary>
		/// The Priority of the item. Ranges from 1-4 where 1 is low and 4 is high priority.
		/// Due to backwards compatibility this property will set priority to 1 for old Todoist items that could have a null value here.
		/// </summary>
		[DataMember(Name = "priority")]
		public int? Priority {
			get
			{
				if (_priority == 0)
				{
					_priority = 1;
				}
				return _priority;
			}
			set
			{
				int? result = value;
				if (result == null)
				{
					result = 1;
				}
				_priority = result.Value;
			}
		}

		[DataMember(Name = "item_order")]
		public int ItemOrder { get; set; }

		[DataMember(Name = "content")]
		public string Content { get; set; }

		[DataMember(Name = "indent")]
		public int Indent { get; set; }

		[DataMember(Name = "project_id")]
		public int ProjectID { get; set; }

		[DataMember(Name = "checked")]
		public bool Checked { get; set; }

		[DataMember(Name = "date_string")]
		public string DateString { get; set; }

		public bool HasTime
		{
			get
			{
				return this.DueDate.HasValue && !String.IsNullOrEmpty(this.DateString) && this.DateString.Contains("at") || this.DateString.Contains("@");
			}
		}

		/// <summary>
		/// Returns a ShortDateString representation of the DueDate. If the item HasTime, an "@" and a ShortTimeString is appended at the end. If DueDate is null, String.Empty is returned.
		/// </summary>
		public string ShowDueDate
		{
			get
			{
				string result = String.Empty;
				if (this.DueDate.HasValue)
				{
					result = this.DueDate.Value.ToShortDateString();

					if (this.HasTime)
					{
						result += " @ " + this.DueDate.Value.ToShortTimeString();
					}
				}

				return result;
			}
		}

		/// <summary>
		/// Gets the project this item belongs to. If the user object has a cached project list, this will be used.
		/// </summary>
		public Project Project
		{
			get
			{
				return this.Owner.GetProjects().Where(p => p.ID == this.ProjectID).Single();
			}
		}

		#endregion


		/// <summary>
		/// The Content of the item. If item HasLink or HasGMail, this property contains only the todo text.
		/// </summary>
		public string TodoText { get; set; }


		#region Links
		/// <summary>
		/// True of this item contains at least one link. Acccess Links for information about the links.
		/// </summary>
		public bool HasLinks
		{
			get
			{
				return this.Links != null && this.Links.Count > 0;
			}
		}

		private List<ItemLink> _links;
		/// <summary>
		/// The links in this item.
		/// </summary>
		public List<ItemLink> Links
		{
			get
			{
				if (_links == null)
				{
					_links = new List<ItemLink>();
				}
				return _links;
			}
		}
		#endregion


		#region GMail
		/// <summary>
		/// True if this item is linked to a GMail e-mail. Access GMailSubject and GMailLinkUrl to display information about the e-mail.
		/// </summary>
		public bool HasGMail
		{
			get
			{
				return !String.IsNullOrEmpty(this.GMailLinkUrl);
			}
		}

		/// <summary>
		/// The subject of the e-mail in an item that is linked to a GMail e-mail.
		/// </summary>
		public string GMailSubject { get; set; }

		/// <summary>
		/// The link to e-mail in an item that is linked to a GMail e-mail.
		/// </summary>
		public string GMailLinkUrl { get; set; } 
		#endregion

		
		#region Item Hierarchy

		//For some reason, initializing here wont work...
		protected List<Item> _items = null;
		//...so using internal Ensure method
		private void EnsureItems()
		{
			if (_items == null)
			{
				_items = new List<Item>();
			}
		}

		/// <summary>
		/// The child items of the current item.
		/// </summary>
		public IEnumerable<Item> Items
		{
			get
			{
				EnsureItems();
				return this._items;
			}
		}

		/// <summary>
		/// The parent item to the current item. Null if this is a root item.
		/// </summary>
		public Item Parent { get; set; }

		//Used to build item hierarchy
		internal void AddChildItem(Item item)
		{
			item.Parent = this;

			EnsureItems();
			_items.Add(item);
		}

		#endregion


		public Item(string content)
		{
			this.Content = content;
		}

		public override void Initialize(User todoistUser)
		{
			base.Initialize(todoistUser);

			this.TodoText = this.Content;
			
			Regex linkRegex = new Regex(@"(http[\S]+) \((.+?)\)");
			MatchCollection linkMatches = linkRegex.Matches(this.Content);
			foreach (Match linkMatch in linkMatches)
			{
				string url = linkMatch.Groups[1].Value;
				string description = linkMatch.Groups[2].Value;

				this.TodoText = this.TodoText.Replace(url, "");
				this.TodoText = this.TodoText.Replace("(" + description + ")", description + " [LINK]");
				this.TodoText = this.TodoText.Trim();

				this.Links.Add(new ItemLink { Url = url, Description = description });
			}

			Regex gmailRegex = new Regex(@"\[\[gmail=([a-z0-9]+)@gmail, (.*)\]\](.*)");		
			if (gmailRegex.IsMatch(this.Content))
			{
			    //Handle gmail items, their Content looks like this:
			    //[[gmail=12c68e48edeae5b1@gmail, gMailSubject]] todoText
				//gmail link looks like this:
				//https://mail.google.com/mail/#inbox/12c68e48edeae5b1

				Match match = gmailRegex.Match(this.Content);

				this.GMailLinkUrl = String.Format("https://mail.google.com/mail/#inbox/{0}", match.Groups[1].Value);
				this.GMailSubject = match.Groups[2].Value;
				this.TodoText = match.Groups[3].Value.Trim();

				this.Content = String.Format("{0} {1}", this.GMailSubject, this.TodoText);
			}
		}


	}
}