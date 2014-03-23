using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Timers;
using System.Windows.Forms;
using Deap.Todoist.Api;
using HabitRPG.NET;
using HabitRPG.NET.Models;

namespace TodoistHabitRPGSync
{
    public partial class Form1 : Form
    {
        private HabitRPGClient _hClient;
        private User _tClient;
        private static System.Timers.Timer aTimer;

        public Form1()
        {
            InitializeComponent();
            var apiUser = ConfigurationManager.AppSettings["habitUserID"];
            var apiToken = ConfigurationManager.AppSettings["habitToken"];
            var tUser = ConfigurationManager.AppSettings["todoistUser"];
            var tPass = ConfigurationManager.AppSettings["todoistPassword"];

            _hClient = new HabitRPGClient("https://habitrpg.com/api/v2/", apiUser, apiToken);
            _tClient = Todoist.Login(tUser, tPass);

            aTimer = new System.Timers.Timer(900000);

            // Hook up the Elapsed event for the timer.
            aTimer.Elapsed += TodoistToHabit;

            // Set the Interval to 2 seconds (2000 milliseconds).
            aTimer.Interval = 900000;
            aTimer.Enabled = true;
            SetText(string.Format("Next update at {0}", DateTime.Now.AddMinutes(15)));
        }

        private void button1_Click(object sender, EventArgs e)
        {        
        }

        public void HabitToTodoist(object source, ElapsedEventArgs e)
        {
            var hTasks = _hClient.GetTasks().Where(x => x.Type == "todo" && (!x.Completed.HasValue || !x.Completed.Value)).ToList();
            var tTasks = _tClient.GetProjects().SelectMany(x => x.GetItems()).ToList();

            foreach (var task in hTasks)
            {
                var existing = tTasks.FirstOrDefault(x => x.Content == task.Text);
                if (existing == null)
                {
                    var newItem = new Item(task.Text);
                    DateTime dueDate;
                    if (task.Date != string.Empty && DateTime.TryParse(task.Date, out dueDate))
                        newItem.DueDate = dueDate;

                    _tClient.AddItem(newItem);
                }
            }
        }

        public void TodoistToHabit(object source, ElapsedEventArgs e)
        {
            var startTime = e.SignalTime;
            SetText("Starting Sync");
            var hTasks = _hClient.GetTasks().Where(x => x.Type == "todo").ToList();
            var tTasks = _tClient.GetProjects().SelectMany(x => x.GetItems()).ToList();
            var newTasks = 0;
            var completedTasks = 0;
            foreach (var task in tTasks)
            {
                var existing = hTasks.FirstOrDefault(x => x.Text == task.Content);
                if (existing == null)
                {
                    if (!task.Checked)
                    {
                        var newItem = new Task
                            {
                                Text = task.Content,
                                Type = "todo",
                                Date = task.DueDate.HasValue ? task.DueDate.Value.ToString("o") : null
                            };
                        _hClient.AddTask(newItem);
                        newTasks++;
                    }
                }
                else
                {
                    if (task.Checked && (bool) (!existing.Completed))
                    {
                        _hClient.ScoreTask(existing.Id, "up");
                        completedTasks++;
                    }
                }
            }
            SetText(string.Format("{0} new tasks. {1} completed tasks. Next update at {2}", newTasks,
                                  completedTasks, startTime.AddMinutes(15)));
        }

        delegate void SetTextCallback(string text);

        private void SetText(string text)
        {
            // InvokeRequired required compares the thread ID of the
            // calling thread to the thread ID of the creating thread.
            // If these threads are different, it returns true.
            if (lblStatusBox.InvokeRequired)
            { 
            SetTextCallback d = SetText;
            Invoke(d, new object[] { text });
            }
            else
            {
                lblStatusBox.Text = text;
            }
        }

    }
}
