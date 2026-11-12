
using System;
using System.Collections.ObjectModel;
using System.Windows;
using CybersecurityAwarenessBot.Models;
using CybersecurityAwarenessBot.Services;

namespace CybersecurityAwarenessBot
{
    public partial class TaskWindow : Window
    {
        private readonly TaskService _taskService;
        private readonly ActivityLogService _activityLog;

        private readonly ObservableCollection<CyberTask> _tasks
            = new();

        public TaskWindow(
            TaskService taskService,
            ActivityLogService activityLog)
        {
            InitializeComponent();

            _taskService = taskService;
            _activityLog = activityLog;

            TasksGrid.ItemsSource = _tasks;

            LoadTasks();
        }

        private void LoadTasks()
        {
            try
            {
                _tasks.Clear();

                foreach (CyberTask task in
                         _taskService.GetAllTasks())
                {
                    _tasks.Add(task);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Could not load tasks: " + ex.Message);
            }
        }

        private void AddTask_Click(
            object sender,
            RoutedEventArgs e)
        {
            string title = TitleBox.Text.Trim();
            string description = DescriptionBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(title))
            {
                MessageBox.Show("Please enter a task title.");
                return;
            }

            try
            {
                DateTime? reminder = ReminderPicker.SelectedDate;

                _taskService.AddTask(
                    title,
                    description,
                    reminder);

                _activityLog.Log(
                    "Task",
                    "Added task: " + title);

                if (reminder.HasValue)
                {
                    _activityLog.Log(
                        "Reminder",
                        "Reminder set for " + title +
                        " on " + reminder.Value.ToString("dd/MM/yyyy"));
                }

                MessageBox.Show(
                    "Your cybersecurity task was saved successfully!");

                TitleBox.Clear();
                DescriptionBox.Clear();
                ReminderPicker.SelectedDate = null;

                LoadTasks();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Error saving task: " + ex.Message);
            }
        }

        private void Refresh_Click(
            object sender,
            RoutedEventArgs e)
        {
            LoadTasks();
        }

        private void Complete_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (TasksGrid.SelectedItem is not CyberTask task)
            {
                MessageBox.Show("Please select a task.");
                return;
            }

            try
            {
                _taskService.MarkCompleted(task.TaskId);

                _activityLog.Log(
                    "Task",
                    "Completed task: " + task.Title);

                LoadTasks();

                MessageBox.Show("Task marked as completed.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Could not complete task: " + ex.Message);
            }
        }

        private void Delete_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (TasksGrid.SelectedItem is not CyberTask task)
            {
                MessageBox.Show("Please select a task.");
                return;
            }

            MessageBoxResult result = MessageBox.Show(
                "Are you sure you want to delete this task?",
                "Confirm deletion",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                _taskService.DeleteTask(task.TaskId);

                _activityLog.Log(
                    "Task",
                    "Deleted task: " + task.Title);

                LoadTasks();

                MessageBox.Show("Task deleted successfully.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Could not delete task: " + ex.Message);
            }
        }
    }
}
