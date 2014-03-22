using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Deap.Todoist.Api;
using HabitRPG.NET;

namespace TodoistHabitRPGSync
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var apiUser = "habit api user ID";
            var apiToken = "habit api token";

            var hClient = new HabitRPGClient("https://habitrpg.com/api/v2/", apiUser, apiToken);
            var tClient = Todoist.Login("todoist email address", "password");


        }

        public void HabitToTodoist(HabitRPGClient hClient, User tClient)
        {
            var hTasks = hClient.GetTasks().Where(x => x.Type == "todo" && (!x.Completed.HasValue || !x.Completed.Value)).ToList();
            var tTasks = tClient.GetProjects().SelectMany(x => x.GetItems()).ToList();

            foreach (var task in hTasks)
            {
                var existing = tTasks.FirstOrDefault(x => x.Content == task.Text);
                if (existing == null)
                {
                    var newItem = new Item(task.Text);
                    DateTime dueDate;
                    if (task.Date != string.Empty && DateTime.TryParse(task.Date, out dueDate))
                        newItem.DueDate = dueDate;

                    tClient.AddItem(newItem);
                }
            }
        }
    }
}
