
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CybersecurityAwarenessBot.Models;

namespace CybersecurityAwarenessBot.Services
{
    public class ActivityLogService
    {
        private readonly List<ActivityLog> _entries = new();

        public int TotalCount => _entries.Count;

        public void Log(string category, string description)
        {
            _entries.Add(new ActivityLog
            {
                Timestamp = DateTime.Now,
                Category = category,
                Description = description
            });
        }

        public List<ActivityLog> GetRecent(int count, int skip)
        {
            return _entries
                .AsEnumerable()
                .Reverse()
                .Skip(skip)
                .Take(count)
                .ToList();
        }

        public string FormatForChat(int count, int skip)
        {
            if (_entries.Count == 0)
            {
                return "I haven't done anything for you yet. " +
                       "Try adding a task or taking the quiz!";
            }

            List<ActivityLog> page = GetRecent(count, skip);

            if (page.Count == 0)
            {
                return "That's everything - there are no older actions to show.";
            }

            StringBuilder sb =
                new StringBuilder("Here's a summary of recent actions:\n");

            int number = skip + 1;

            foreach (ActivityLog entry in page)
            {
                sb.AppendLine(
                    number + ". " +
                    entry.Category + ": " +
                    entry.Description + " [" +
                    entry.Timestamp.ToString("dd MMM HH:mm") + "]");

                number++;
            }

            int remaining = _entries.Count - (skip + page.Count);

            if (remaining > 0)
            {
                sb.AppendLine(
                    "\n(" + remaining +
                    " older action(s) available - type 'show more'.)");
            }

            return sb.ToString().TrimEnd();
        }
    }
}
