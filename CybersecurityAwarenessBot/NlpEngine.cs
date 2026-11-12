using System.Globalization;
using System.Text.RegularExpressions;

namespace CybersecurityAwarenessBot.Services;

public enum BotIntent { None, AddTask, SetReminder, ViewTasks, CompleteTask, DeleteTask, StartQuiz, ShowLog, ShowMoreLog }

/// <summary>What the NLP simulation understood from a sentence.</summary>
public class NlpResult
{
    public BotIntent Intent { get; set; } = BotIntent.None;
    public string TaskTitle { get; set; } = string.Empty;
    public string TargetText { get; set; } = string.Empty;
    public DateTime? ReminderDate { get; set; }
}

/// <summary>
/// NLP simulation using keyword detection and regular expressions.
/// It copes with many phrasings ("Add a task to enable 2FA", "Can you remind me to update my password tomorrow?").
/// </summary>
public class NlpEngine
{
    private const RegexOptions Ci = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;
    private const string Units = "minute|hour|day|week|month";
    private const string Numbers = @"\d+|a|an|one|two|three|four|five|six|seven|eight|nine|ten";

    private static readonly Dictionary<string, int> NumberWords = new()
    {
        ["a"] = 1,
        ["an"] = 1,
        ["one"] = 1,
        ["two"] = 2,
        ["three"] = 3,
        ["four"] = 4,
        ["five"] = 5,
        ["six"] = 6,
        ["seven"] = 7,
        ["eight"] = 8,
        ["nine"] = 9,
        ["ten"] = 10
    };

    private static readonly Regex[] AddTaskPatterns =
    {
        new(@"\b(?:add|create|make|new|set\s*up)\s+(?:a\s+|another\s+|the\s+)?(?:new\s+)?task\b\s*(?:-|:|\bto\b|\bfor\b|\bcalled\b|\bnamed\b|\babout\b)?\s*(?<title>.*)$", Ci)
    };

    private static readonly Regex[] ReminderPatterns =
    {
        new($@"\bremind\s+me\s+(?:in\s+(?:{Numbers})\s+(?:{Units})s?|tomorrow|today|tonight|next\s+\w+)\s+(?:to|about|of)\s+(?<title>.+)$", Ci),
        new(@"\bremind\s+me\s+(?:to|about|of)\s+(?<title>.+)$", Ci),
        new(@"\b(?:set|add|create|make)\s+(?:a\s+|another\s+)?reminder\s*(?:-|:|\bto\b|\bfor\b|\babout\b)\s*(?<title>.+)$", Ci)
    };

    private static readonly Regex[] TimePhrasePatterns =
    {
        new($@"\bin\s+(?:{Numbers})\s+(?:{Units})s?\b", Ci),
        new($@"\b\d+\s+(?:{Units})s?\s+from\s+now\b", Ci),
        new(@"\b(?:tomorrow|today|tonight)\b", Ci),
        new(@"\bnext\s+(?:week|month)\b", Ci),
        new(@"\bon\s+\d{1,2}[/\-.]\d{1,2}[/\-.]\d{2,4}\b", Ci),
        new(@"\b\d{1,2}[/\-.]\d{1,2}[/\-.]\d{2,4}\b", Ci)
    };

    /// <summary>Works out what the user wants (intent) plus any task title / reminder date in the sentence.</summary>
    public NlpResult Analyse(string input)
    {
        string text = input.Trim();
        string lower = text.ToLowerInvariant();
        var result = new NlpResult { ReminderDate = ParseReminderDate(text) };

        // 1. Activity log
        if (Regex.IsMatch(lower, @"activity log|what have you done|what did you do|show (?:the )?log|recent actions|action log"))
        {
            result.Intent = BotIntent.ShowLog;
            return result;
        }
        if (Regex.IsMatch(lower, @"\bshow\s+more\b|\bshow\s+(?:the\s+)?(?:full|all)\s+(?:log|history)\b"))
        {
            result.Intent = BotIntent.ShowMoreLog;
            return result;
        }

        // 2. Add a task
        foreach (Regex pattern in AddTaskPatterns)
        {
            Match m = pattern.Match(text);
            if (m.Success)
            {
                result.Intent = BotIntent.AddTask;
                result.TaskTitle = CleanTitle(m.Groups["title"].Value);
                return result;
            }
        }

        // 3. Reminders ("remind me to ...", "add a reminder to ...")
        foreach (Regex pattern in ReminderPatterns)
        {
            Match m = pattern.Match(text);
            if (m.Success)
            {
                result.Intent = BotIntent.SetReminder;
                result.TaskTitle = CleanTitle(m.Groups["title"].Value);
                return result;
            }
        }

        // 4. Complete / delete an existing task
        bool mentionsTask = Regex.IsMatch(lower, @"\btasks?\b|^(?:complete|finish|delete|remove|mark)\s+#?\d+\b|\bmark\b.+\b(?:done|complete|completed|finished)\b");
        if (mentionsTask && Regex.IsMatch(lower, @"\b(?:complete|completed|finish|finished|done|mark)\b"))
        {
            result.Intent = BotIntent.CompleteTask;
            result.TargetText = ExtractTarget(lower);
            return result;
        }
        if (mentionsTask && Regex.IsMatch(lower, @"\b(?:delete|remove|cancel)\b"))
        {
            result.Intent = BotIntent.DeleteTask;
            result.TargetText = ExtractTarget(lower);
            return result;
        }

        // 5. View tasks
        if (Regex.IsMatch(lower, @"\b(?:show|view|list|see|display|open|check)\b.*\b(?:tasks?|reminders?)\b|\bmy (?:tasks?|reminders?)\b|\bwhat tasks\b|\btask list\b"))
        {
            result.Intent = BotIntent.ViewTasks;
            return result;
        }

        // 6. Quiz
        if (Regex.IsMatch(lower, @"\b(?:quiz|mini-?game|game|trivia|test (?:me|my knowledge))\b"))
        {
            result.Intent = BotIntent.StartQuiz;
            return result;
        }

        // 7. "task" / "reminder" mentioned without a clear sentence: ask what the task is
        if (Regex.IsMatch(lower, @"\b(?:reminder|remind|task)\b"))
        {
            result.Intent = BotIntent.AddTask;
            return result;
        }

        return result;
    }

    /// <summary>Understands "in 3 days", "in two weeks", "tomorrow", "next week", "25/12/2026", "5 days from now".</summary>
    public DateTime? ParseReminderDate(string input)
    {
        string text = input.ToLowerInvariant();

        Match rel = Regex.Match(text, $@"\bin\s+(?<n>{Numbers})\s+(?<unit>{Units})s?\b");
        if (!rel.Success)
            rel = Regex.Match(text, $@"\b(?<n>\d+)\s+(?<unit>{Units})s?\s+from\s+now\b");

        if (rel.Success)
        {
            int n = ToNumber(rel.Groups["n"].Value);
            DateTime now = DateTime.Now;
            return rel.Groups["unit"].Value switch
            {
                "minute" => now.AddMinutes(n),
                "hour" => now.AddHours(n),
                "day" => now.AddDays(n),
                "week" => now.AddDays(7 * n),
                _ => now.AddMonths(n)
            };
        }

        if (Regex.IsMatch(text, @"\btomorrow\b")) return DateTime.Today.AddDays(1).AddHours(9);
        if (Regex.IsMatch(text, @"\btonight\b"))
        {
            DateTime tonight = DateTime.Today.AddHours(20);
            return tonight > DateTime.Now ? tonight : DateTime.Now.AddHours(1);
        }
        if (Regex.IsMatch(text, @"\btoday\b")) return DateTime.Now.AddHours(1);
        if (Regex.IsMatch(text, @"\bnext\s+week\b")) return DateTime.Today.AddDays(7).AddHours(9);
        if (Regex.IsMatch(text, @"\bnext\s+month\b")) return DateTime.Today.AddMonths(1).AddHours(9);

        Match date = Regex.Match(text, @"\b\d{1,2}[/\-.]\d{1,2}[/\-.]\d{2,4}\b");
        if (date.Success &&
            DateTime.TryParse(date.Value.Replace('-', '/').Replace('.', '/'), new CultureInfo("en-GB"),
                              DateTimeStyles.None, out DateTime parsed))
        {
            return parsed.Date.AddHours(9);
        }

        return null;
    }

    /// <summary>Removes time phrases and filler from a task title and capitalises it.</summary>
    public string CleanTitle(string raw)
    {
        string title = raw;
        foreach (Regex pattern in TimePhrasePatterns)
            title = pattern.Replace(title, " ");

        title = Regex.Replace(title, @"\s+", " ").Trim();
        title = Regex.Replace(title, @"\s+(?:and|by|on|at)$", string.Empty, Ci);
        title = title.Trim(' ', '.', '?', '!', ',', ';', ':', '-');
        return Capitalise(title);
    }

    public static string Capitalise(string text) =>
        string.IsNullOrEmpty(text) ? text : char.ToUpper(text[0]) + text[1..];

    private static string ExtractTarget(string lower)
    {
        Match number = Regex.Match(lower, @"#?\b(\d+)\b");
        if (number.Success) return number.Groups[1].Value;

        string target = Regex.Replace(lower,
            @"\b(?:please|can you|could you|i have|i've|mark|as|complete|completed|finish|finished|done|delete|remove|cancel|the|task|tasks|my|it|that|a|to|with)\b",
            " ");
        return Regex.Replace(target, @"\s+", " ").Trim(' ', '.', '?', '!');
    }

    private static int ToNumber(string value) =>
        int.TryParse(value, out int n) ? n : NumberWords.GetValueOrDefault(value, 1);
}