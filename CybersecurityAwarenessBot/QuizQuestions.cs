
using System.Collections.Generic;

namespace CybersecurityAwarenessBot.Models
{
    public class QuizQuestion
    {
        public string Text { get; set; } = string.Empty;

        public List<string> Options { get; set; } = new();

        public int CorrectIndex { get; set; }

        public string Explanation { get; set; } = string.Empty;
    }
}
