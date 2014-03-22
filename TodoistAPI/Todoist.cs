using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Web;

namespace Deap.Todoist.Api
{
	/// <summary>
	/// Core class containing Login functionality and methods to make requests to the Todoist API.
	/// </summary>
	public static class Todoist
	{
		/// <summary>
		/// Login user into Todoist to get a user object with a token, needed to do additional calls to the Todoist API.
		/// </summary>
		/// <param name="email">User's email</param>
		/// <param name="password">User's password</param>
		/// <returns>Returns a Todoist.User object where all properties are assigned values from the user account.</returns>
		public static User Login(string email, string password)
		{
			return Request<User>("login", String.Format("email={0}&password={1}", email, password), null);
		}

		/// <summary>
		/// Login user into Todoist to get a user object with a token, needed to do additional calls to the Todoist API.
		/// Makes a test call to the Api, if exception is thrown it indicates that the API token is wrong.
		/// </summary>
		/// <param name="apiToken">The users Todoist API token.</param>
		/// <returns>Returns a Todoist.User object where only the ApiToken property is assigned. To get a user object with users settings you must use Login(string, string).</returns>
		/// <exception cref="TodoistException"/>
		public static User Login(string apiToken)
		{
			User u = new User(apiToken);

			//Testing to make call, throws exception if apiToken is wrong.
			u.Query(DateTime.Today);

			return u;
		}

		internal static T Request<T>(string command, User todoistUser) where T : class
		{
			string parameters = string.Empty;
			return Request<T>(command, parameters, todoistUser);
		}

		internal static T Request<T>(string command, string parameters, User todoistUser) where T : class
		{
			string apiToken = todoistUser == null ? "" : todoistUser.ApiToken; //apiToken is required for all commands except "login".

			MemoryStream ms = MakeRequest(command, parameters, apiToken);
			if (ms != null)
			{
				T result = Deserialize<T>(ms, command);

				if (result is TodoistObject)
				{
					(result as TodoistObject).Initialize(todoistUser);
				}
				else if (result is IEnumerable<TodoistObject>)
				{
					foreach (TodoistObject item in (IEnumerable<TodoistObject>)result)
					{
						item.Initialize(todoistUser);
					}
				}
				return result;
			}
			else
			{
				return null;
			}
		}

		private static MemoryStream MakeRequest(string command, string parameters, string apiToken)
		{
			string apiTokenParam = string.Empty;
			if(!String.IsNullOrEmpty(apiToken))
			{
				apiTokenParam = String.IsNullOrEmpty(parameters) ? "" : "&";
				apiTokenParam += "token=" + apiToken;
			}

			string commandUrl = "https://todoist.com/API/" + command + "?" + parameters + apiTokenParam;

			try
			{
				WebClient wc = new WebClient();
				byte[] originalData = wc.DownloadData(commandUrl);
				MemoryStream result = new MemoryStream(originalData);
				result.Position = 0;
				return result;
			}
			catch (Exception ex)
			{
				string errorMessage = "Failed to execute command " + commandUrl;
				string errorDetails = "Unknown error";

				WebException webException = ex as WebException;
				
				if (webException != null)
				{
					HttpWebResponse response = (HttpWebResponse)webException.Response;
					if (response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.InternalServerError)
					{
						errorDetails = new StreamReader(response.GetResponseStream()).ReadToEnd();
					}
				}
				
				throw new TodoistException(errorMessage + " " + errorDetails, ex);				
			}
		}

		private static T Deserialize<T>(MemoryStream ms, string command) where T : class
		{
			try
			{
				DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(T));
				T result = (T)ser.ReadObject(ms);
				return result;
			}
			catch (Exception ex)
			{
				ms.Position = 0;
				string todoistError = new StreamReader(ms).ReadToEnd().Replace("\"", "");
				throw new TodoistException(todoistError, ex);
			}
		}
	}
}
