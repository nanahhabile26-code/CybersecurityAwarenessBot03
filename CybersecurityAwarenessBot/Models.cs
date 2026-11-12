using System;

namespace CybersecurityAwarenessBot.Models
{
    /// <summary>One cybersecurity task stored in the MySQL CyberTasks table.</summary>
    public class CyberTask
    {
        public int TaskId { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public DateTime? ReminderDate { get; set; }

        public bool IsCompleted { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Used by the DataGrid bindings in TaskWindow.xaml
        public string StatusText => IsCompleted ? "Completed" : "Pending";

        public string ReminderText =>
            ReminderDate.HasValue ? ReminderDate.Value.ToString("dd/MM/yyyy HH:mm") : "No reminder set";

        public override string ToString() => $"{Title} | {StatusText} | Reminder: {ReminderText}";
    }
}