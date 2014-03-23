using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Globalization;
using System.Web;

namespace Deap.Todoist.Api
{
	[DataContract]
	public class User : TodoistIDObject
	{
		#region Properties and API mappings
		/// <summary>
		/// Gets or sets user's email. Call User.Update to persist.
		/// </summary>
		[DataMember(Name = "email")]
		public string Email { get; set; }

		/// <summary>
		/// Gets or sets user's real name. Call User.Update to persist.
		/// </summary>
		[DataMember(Name = "full_name")]
		public string FullName { get; set; }

		/// <summary>
		/// Gets user's token (that is needed to call the other methods). 
		/// </summary>
		[DataMember(Name = "api_token")]
		public string ApiToken { get; set; }

		/// <summary>
		/// Gets user's default view on Todoist.com.
		/// </summary>
		[DataMember(Name = "start_page")]
		public string StartPage { get; internal set; }

		/// <summary>
		/// Gets or sets user's timezone in a string. Must be one of the values returned from Todoist.GetTimeZones. Call User.Update to persist.
		/// </summary>
		[DataMember(Name = "timezone")]
		public string TimeZone { get; internal set; }

		[DataMember(Name = "tz_offset")]
		internal object[] _timeZoneOffsetMapper { get; set; }

		private TimeZoneOffset _timeZoneOffset;
		/// <summary>
		/// User's timezone offset.
		/// </summary>		
		public TimeZoneOffset TimeZoneOffset
		{
			get
			{
				if (_timeZoneOffset == null)
				{
					_timeZoneOffset = new TimeZoneOffset(_timeZoneOffsetMapper[0].ToString(), Convert.ToInt32(_timeZoneOffsetMapper[1]), Convert.ToInt32(_timeZoneOffsetMapper[2]), Convert.ToBoolean(_timeZoneOffsetMapper[3]));
				}
				return _timeZoneOffset;
			}
		}

		/// <summary>
		/// true for am/pm clock, false for 24 hour clock.
		/// </summary>
		[DataMember(Name = "time_format")]
		public bool AmPmTime { get; internal set; }

		/// <summary>
		/// true for dateformat MM-DD-YYYY, false for dateformat DD-MM-YYYY.
		/// </summary>
		[DataMember(Name = "date_format")]
		public bool MonthFirstInDates { get; internal set; }

		/// <summary>
		/// true for showing oldest dates last, false for showing oldest dates first.
		/// </summary>
		[DataMember(Name = "sort_order")]
		public bool OldestDatesLast { get; internal set; }

		/// <summary>
		/// User's Twitter account. Empty string ("") if not set. 
		/// </summary>
		[DataMember(Name = "twitter")]
		public string Twitter { get; internal set; }

		/// <summary>
		/// User's Jabber account. Empty string ("") if not set. 
		/// </summary>
		[DataMember(Name = "jabber")]
		public string Jabber { get; internal set; }

		/// <summary>
		/// User's MSN account. Empty string ("") if not set. 
		/// </summary>
		[DataMember(Name = "msn")]
		public string Msn { get; internal set; }

		/// <summary>
		/// User's mobile number.
		/// </summary>
		[DataMember(Name = "mobile_number")]
		public string MobileNumber { get; internal set; }

		/// <summary>
		/// User's mobile host.
		/// </summary>
		[DataMember(Name = "mobile_host")]
		public string MobileHost { get; internal set; }

		[DataMember(Name = "premium_until")]
		internal string _premiumUntil { get; set; }

		public DateTime PremiumUntil
		{
			get
			{
				return DateTime.ParseExact(_premiumUntil, "ddd MMM d HH:mm:ss yyyy", CultureInfo.InvariantCulture);
			}
		}

		[DataMember(Name = "default_reminder")]
		internal string _defaultReminder { get; set; }

		/// <summary>
		/// What is the default reminder for the user? Reminders are only possible for premium users.
		/// </summary>
		public ReminderType DefaultReminder
		{
			get
			{
				ReminderType result;

				switch (this._defaultReminder)
				{
					case "email":
						result = ReminderType.Email;
						break;
					case "mobile":
						result = ReminderType.Mobile;
						break;
					case "twitter":
						result = ReminderType.Twitter;
						break;
					default:
						result = ReminderType.NoDefault;
						break;
				}

				return result;
			}
		} 
		#endregion

		#region Private
		/// <summary>
		/// After the first project request, this holds an in memory cached list of the users projects.
		/// </summary>
		private Project[] _projects = null;

		/// <summary>
		/// After the first project request, this holds an in memory cached list of the users root projects.
		/// </summary>
		private Project[] _rootProjects = null; 
		#endregion

		#region Constructor

		/// <summary>
		/// This constructor is internal. To get a user object externally, you need to call any of the Todoist.Login methods.
		/// </summary>
		/// <param name="apiToken"></param>
		internal User(string apiToken)
		{
			this.ApiToken = apiToken;
		} 
		#endregion


		#region Project
		public Project GetProject(int projectID)
		{
			bool refresh = false;
			return GetProject(projectID, refresh);
		}

		public Project GetProject(int projectID, bool refresh)
		{
			if (refresh)
			{
				//If refresh, call API.
				return Todoist.Request<Project>("getProject", "project_id=" + projectID, this);
			}
			else
			{
				//If not, use cached project list
				Project[] projects = GetProjects();
				return projects.Where(p => p.ID == projectID).Single();
			}
		}

		public Project[] GetRootProjects()
		{
			return GetRootProjects(false);
		}

		/// <summary>
		/// Gets all root projects for the current user.
		/// </summary>
		/// <param name="refresh">True to force data fetch from the Todoist server.</param>
		public Project[] GetRootProjects(bool refresh)
		{
			if (_rootProjects == null || refresh)
			{
				return GetProjects(true, refresh);
			}
			return _rootProjects;
		}

		public Project[] GetProjects()
		{
			return GetProjects(false, false);
		}

		/// <summary>
		/// Gets all projects for the current user.
		/// </summary>
		/// <param name="refresh">True to force data fetch from the Todoist server.</param>
		public Project[] GetProjects(bool refresh)
		{
			return GetProjects(false, refresh);
		}

		private Project[] GetProjects(bool rootProjectsOnly, bool refresh)
		{
			if (_projects == null || refresh)
			{
				_projects = Todoist.Request<Project[]>("getProjects", this);
			}

			BuildProjectHierarchy();

			if (rootProjectsOnly)
			{
				return _rootProjects;
			}

			return _projects;
		}

		private void BuildProjectHierarchy()
		{
			List<Project> rootProjects = new List<Project>();
			Project previousProject = null;
			foreach (Project project in _projects)
			{
				if (project.Indent == 1)
				{
					rootProjects.Add(project);
				}
				else if (previousProject != null)
				{
					if (project.Indent > previousProject.Indent)
					{
						previousProject.AddChildProject(project);
					}
					else if (project.Indent == previousProject.Indent)
					{
						previousProject.Parent.AddChildProject(project);
					}
					else if (project.Indent < previousProject.Indent)
					{
						Project previousSibling = previousProject.Parent;
						while (project.Indent != previousSibling.Indent)
						{
							previousSibling = previousSibling.Parent;
						}
						previousSibling.Parent.AddChildProject(project);
					}
				}
				previousProject = project;
			}
			_rootProjects = rootProjects.ToArray();
		} 
		#endregion
		
		
		#region Item
		
		[Obsolete("Not working, see workitem http://todoistapi.codeplex.com/workitem/7829", true)]
		public Item[] GetItemsByID(params long[] itemIDs)
		{
			string[] strItemIDs = new string[itemIDs.Length];
			for (int i = 0; i < itemIDs.Length; i++)
			{
				strItemIDs[i] = itemIDs[i].ToString();
			}

			string commaSepItemIDs = String.Join(",", strItemIDs);

			return Todoist.Request<Item[]>("getItemsById", "ids=[" + commaSepItemIDs +"]", this);
		}

        public Item AddItem(Item newItem)
        {
            var parameters = "content=" + newItem.Content;
            if (newItem.DueDate != null)
                parameters += "&date_string=" + newItem.DueDate.Value.ToShortDateString();
            if (newItem.Priority != null)
                parameters += "&priority=" + newItem.Priority;
            else
                parameters += "&priority=4";
            return Todoist.Request<Item>("addItem", parameters, this);
        }

		#endregion


		#region Query

		private const string QUERY_DATE_FORMAT = "yyyy-M-dTHH:mm"; //'2010-11-16T00:00'

		public QueryResult[] Query(int days)
		{
			return Query(DateTime.Today, DateTime.Today.AddDays(days));
		}

		public QueryResult[] Query(DateTime from, DateTime to)
		{	
			TimeSpan subtract = to.Subtract(from);
			int days = (int)subtract.TotalDays;
			string[] dates = new string[days];
			for (int i = 0; i < days; i++)
			{
				DateTime date = from.AddDays(i);
				dates[i] = date.ToString(QUERY_DATE_FORMAT);
			}

			return Query(dates);
		}

		public QueryResult Query(DateTime date)
		{
			return Query(date.ToString(QUERY_DATE_FORMAT))[0];
		}

		public QueryResult[] Query(params string[] queries)
		{
			string[] quotedQueries = new string[queries.Length];
			for (int i = 0; i < queries.Length; i++)
			{
				quotedQueries[i] = "%22" + queries[i] + "%22";
			}
			return Todoist.Request<QueryResult[]>("query", "queries=[" + String.Join(",", quotedQueries) + "]", this);
		} 
		#endregion

	}
}