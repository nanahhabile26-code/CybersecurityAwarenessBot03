using System;

namespace CybersecurityAwarenessBot.Models
{
    public class ActivityLog
    {
        public int Id { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.Now;

        public string Category { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;
    }
}