using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CybersecurityAwarenessBot.Models;

namespace CybersecurityAwarenessBot.Services
{
    /// <summary>What the command processor decided to do with a chat message.</summary>
    public class CommandResult
    {
        /// <summary>False means "not a Part 3 command": pass the message to ChatEngine instead.</summary>
        public bool Handled { get; set; }
        public string Reply { get; set; } = string.Empty;
        public bool OpenQuiz { get; set; }
    }

    /// <summary>
    /// Part 3 NLP simulation. Understands tasks, reminders, quiz and activity-log requests typed in
    /// natural language (keyword detection + regular expressions via NlpEngine) and runs them.
    /// ChatEngine keeps handling the Part 1/2 topics, memory and sentiment.
    /// </summary>
    public class CommandProcessor
    {
        private const int PageSize = 5;
        private const string DatabaseDownMessage =
            "I can't reach the task database right now, so I can't manage tasks. " +
            "Please check that MySQL is running and the password in Services.cs is correct.";

        private readonly TaskService? _taskService;
        private readonly ActivityLogService _activityLog;
        private readonly NlpEngine _nlp = new();
        private readonly HashSet<int> _notified = new();

        private int? _pendingReminderTaskId;
        private string _pendingTitle = string.Empty;
        private bool _awaitingTaskTitle;
        private bool _awaitingIsReminder;
        private bool _lastWasLog;
        private int _logShown;

        public CommandProcessor(TaskService? taskService, ActivityLogService activityLog)
        {
            _taskService = taskService;
            _activityLog = activityLog;
        }

        // ------------------------------------------------------------ public API

        public CommandResult Handle(string input)
        {
            string text = input.Trim().Replace('\u2019', '\'');
            bool wasLog = _lastWasLog;
            _lastWasLog = false;

            // "Would you like a reminder?" -> yes / no / "in 3 days"
            if (_pendingReminderTaskId.HasValue)
            {
                CommandResult? answer = HandlePendingReminder(text);
                if (answer != null) return answer;
            }

            NlpResult nlp = _nlp.Analyse(text);

            // "What should the task be?" -> the next message is the title
            if (_awaitingTaskTitle)
            {
                _awaitingTaskTitle = false;
                if (nlp.Intent == BotIntent.None)
                    return CreateTask(_nlp.CleanTitle(text), _nlp.ParseReminderDate(text), _awaitingIsReminder);
            }

            switch (nlp.Intent)
            {
                case BotIntent.AddTask: return HandleAddTask(nlp, text, false);
                case BotIntent.SetReminder: return HandleAddTask(nlp, text, true);
                case BotIntent.ViewTasks: return ViewTasks();
                case BotIntent.CompleteTask: return CompleteTask(nlp, text);
                case BotIntent.DeleteTask: return DeleteTask(nlp, text);
                case BotIntent.StartQuiz:
                    return Reply("Let's test your cybersecurity knowledge! Opening the quiz...", true);
                case BotIntent.ShowLog: return Reply(ShowLogFirstPage());
                case BotIntent.ShowMoreLog when wasLog: return Reply(ShowLogNextPage());
                default: return new CommandResult();      // not handled here
            }
        }

        /// <summary>First page (5 newest actions). Also used by the Activity Log button.</summary>
        public string ShowLogFirstPage()
        {
            _logShown = PageSize;
            _lastWasLog = true;
            return _activityLog.FormatForChat(PageSize, 0);
        }

        /// <summary>Next 5 older actions ("show more").</summary>
        public string ShowLogNextPage()
        {
            int skip = _logShown;
            _logShown += PageSize;
            _lastWasLog = true;
            return _activityLog.FormatForChat(PageSize, skip);
        }

        /// <summary>Tasks whose reminder time has passed (each is returned only once).</summary>
        public List<CyberTask> TakeDueReminders()
        {
            var due = new List<CyberTask>();
            if (_taskService == null) return due;

            try
            {
                foreach (CyberTask task in _taskService.GetAllTasks())
                {
                    if (!task.IsCompleted && task.ReminderDate.HasValue &&
                        task.ReminderDate.Value <= DateTime.Now && _notified.Add(task.TaskId))
                    {
                        due.Add(task);
                    }
                }
            }
            catch (Exception)
            {
                // database hiccup: try again on the next timer tick
            }
            return due;
        }

        // ------------------------------------------------------------ tasks

        private CommandResult HandleAddTask(NlpResult nlp, string input, bool isReminder)
        {
            if (string.IsNullOrWhiteSpace(nlp.TaskTitle))
            {
                _awaitingTaskTitle = true;
                _awaitingIsReminder = isReminder;
                return Reply(isReminder
                    ? "Sure! What would you like me to remind you about?"
                    : "Sure! What should the task be? For example: 'Enable two-factor authentication'.");
            }
            return CreateTask(nlp.TaskTitle, nlp.ReminderDate, isReminder);
        }

        private CommandResult CreateTask(string title, DateTime? date, bool isReminder)
        {
            if (string.IsNullOrWhiteSpace(title))
                return Reply("I couldn't work out the task title. Try 'Add task - <what you need to do>'.");
            if (_taskService == null)
                return Reply(DatabaseDownMessage);

            string description = BuildDescription(title);
            try
            {
                int id = _taskService.AddTask(title, description, date);
                _activityLog.Log("Task", date.HasValue
                    ? $"Task added: '{title}' (reminder set for {date.Value:dd MMM yyyy HH:mm}) [via NLP]"
                    : $"Task added: '{title}' (no reminder set) [via NLP]");

                if (date.HasValue)
                {
                    string when = $"{date.Value:dddd, dd MMM yyyy} at {date.Value:HH:mm}";
                    return Reply(isReminder
                        ? $"Reminder set for '{title}' on {when}."
                        : $"Task added: '{title}'. {description} I've set a reminder for {when}.");
                }

                _pendingReminderTaskId = id;
                _pendingTitle = title;
                return Reply(isReminder
                    ? $"I've saved '{title}'. When should I remind you? (e.g. 'in 3 days' or 'tomorrow')"
                    : $"Task added: '{title}'. {description} Would you like a reminder? (e.g. 'Yes, remind me in 3 days' or 'No')");
            }
            catch (Exception ex)
            {
                return Reply("Sorry, I couldn't save that task: " + ex.Message);
            }
        }

        private CommandResult? HandlePendingReminder(string text)
        {
            if (_taskService == null)
            {
                _pendingReminderTaskId = null;
                return null;
            }

            string lower = text.ToLowerInvariant();

            if (Regex.IsMatch(lower, @"^(?:no|nope|nah|no thanks|not now|skip)\b"))
            {
                _pendingReminderTaskId = null;
                return Reply("No problem. The task is saved without a reminder.");
            }

            DateTime? when = _nlp.ParseReminderDate(lower);
            if (when.HasValue)
            {
                int id = _pendingReminderTaskId!.Value;
                _pendingReminderTaskId = null;
                try
                {
                    _taskService.SetReminder(id, when);
                    _activityLog.Log("Reminder", $"Reminder set: '{_pendingTitle}' on {when.Value:dd MMM yyyy HH:mm} [via NLP]");
                    return Reply($"Got it! I'll remind you on {when.Value:dddd, dd MMM yyyy} at {when.Value:HH:mm}.");
                }
                catch (Exception ex)
                {
                    return Reply("Sorry, I couldn't save that reminder: " + ex.Message);
                }
            }

            if (Regex.IsMatch(lower, @"^(?:yes|yeah|yep|sure|ok|okay|please|y)\b"))
                return Reply("Great! When should I remind you? For example: 'in 3 days', 'tomorrow' or '25/12/2026'.");

            _pendingReminderTaskId = null;   // user moved on to something else
            return null;
        }

        private CommandResult ViewTasks()
        {
            if (_taskService == null) return Reply(DatabaseDownMessage);

            try
            {
                List<CyberTask> tasks = _taskService.GetAllTasks();
                if (tasks.Count == 0)
                    return Reply("You don't have any tasks yet. Try 'Add task - Review privacy settings'.");

                var sb = new StringBuilder("Here are your tasks:");
                foreach (CyberTask t in tasks)
                    sb.Append($"\n#{t.TaskId} [{t.StatusText}] {t.Title} - {t.Description} (Reminder: {t.ReminderText})");
                return Reply(sb.ToString());
            }
            catch (Exception ex)
            {
                return Reply("Sorry, I couldn't load your tasks: " + ex.Message);
            }
        }

        private CommandResult CompleteTask(NlpResult nlp, string input)
        {
            if (_taskService == null) return Reply(DatabaseDownMessage);
            try
            {
                CyberTask? task = FindTask(_taskService.GetAllTasks(), nlp.TargetText);
                if (task == null)
                    return Reply("I couldn't find that task. Say 'show my tasks' to see their numbers, then try 'complete task 2'.");
                _taskService.MarkCompleted(task.TaskId);
                _activityLog.Log("Task", $"Task completed: '{task.Title}' [via NLP]");
                return Reply($"Nice work! '{task.Title}' is now marked as completed.");
            }
            catch (Exception ex)
            {
                return Reply("Sorry, I couldn't do that: " + ex.Message);
            }
        }

        private CommandResult DeleteTask(NlpResult nlp, string input)
        {
            if (_taskService == null) return Reply(DatabaseDownMessage);
            try
            {
                CyberTask? task = FindTask(_taskService.GetAllTasks(), nlp.TargetText);
                if (task == null)
                    return Reply("I couldn't find that task. Say 'show my tasks' to see their numbers, then try 'delete task 2'.");
                _taskService.DeleteTask(task.TaskId);
                _activityLog.Log("Task", $"Task deleted: '{task.Title}' [via NLP]");
                return Reply($"Done. '{task.Title}' has been deleted.");
            }
            catch (Exception ex)
            {
                return Reply("Sorry, I couldn't delete it: " + ex.Message);
            }
        }

        // ------------------------------------------------------------ helpers

        private static CommandResult Reply(string text, bool openQuiz = false) =>
            new CommandResult { Handled = true, Reply = text, OpenQuiz = openQuiz };

        private static CyberTask? FindTask(List<CyberTask> tasks, string target)
        {
            if (string.IsNullOrWhiteSpace(target)) return null;
            if (int.TryParse(target, out int id)) return tasks.FirstOrDefault(t => t.TaskId == id);
            return tasks.FirstOrDefault(t => t.Title.Contains(target, StringComparison.OrdinalIgnoreCase));
        }

        private static string BuildDescription(string title)
        {
            string t = title.ToLowerInvariant();
            if (t.Contains("privacy")) return "Review account privacy settings to ensure your data is protected.";
            if (t.Contains("2fa") || t.Contains("two-factor") || t.Contains("two factor") || t.Contains("authenticat"))
                return "Enable two-factor authentication to add an extra layer of protection to your accounts.";
            if (t.Contains("password")) return "Update or strengthen your passwords so every account has a strong, unique one.";
            if (t.Contains("update") || t.Contains("software")) return "Install the latest security updates to fix known vulnerabilities.";
            if (t.Contains("backup") || t.Contains("back up")) return "Back up important files so you can recover from ransomware or device loss.";
            return $"Cybersecurity task: {title}.";
        }

        private static string Shorten(string text) => text.Length <= 50 ? text : text.Substring(0, 47) + "...";
    }
}