
using System.Collections.Generic;
using CybersecurityAwarenessBot.Models;

namespace CybersecurityAwarenessBot.Services
{
    public class QuizService
    {
        private readonly List<QuizQuestion> _questions = new();

        private int _currentIndex;
        private int _score;

        public int TotalQuestions => _questions.Count;
        public int CurrentNumber => _currentIndex + 1;
        public int Score => _score;

        public bool IsLastQuestion =>
            _currentIndex == _questions.Count - 1;

        public QuizQuestion Current =>
            _questions[_currentIndex];

        public QuizService()
        {
            LoadQuestions();
        }

        private void LoadQuestions()
        {
            _questions.Clear();

            Add("What is phishing?",
                new[] { "Computer hardware", "A fraudulent attempt to steal information", "A backup method", "A programming language" },
                1, "Phishing uses fake messages to steal sensitive information.");

            Add("What makes a password strong?",
                new[] { "Your birthday", "123456", "A long unique combination", "Your name" },
                2, "Long and unique passwords are harder to guess.");

            Add("What should you do with a suspicious email?",
                new[] { "Click its links", "Reply immediately", "Share it", "Avoid links and report it" },
                3, "Suspicious emails should be reported.");

            Add("What is two-factor authentication?",
                new[] { "Two usernames", "An additional verification step", "Sharing passwords", "Disabling security" },
                1, "2FA adds another layer of account protection.");

            Add("Why should software be updated?",
                new[] { "To fix security vulnerabilities", "To delete files", "To share data", "To disable security" },
                0, "Updates often fix security weaknesses.");

            Add("Is public Wi-Fi always safe for banking?",
                new[] { "True", "False" },
                1, "Public Wi-Fi can expose your information.");

            Add("What is malware?",
                new[] { "A security update", "Harmful software", "A password manager", "A browser" },
                1, "Malware is software designed to harm or spy on devices.");

            Add("Should you share your OTP with a caller?",
                new[] { "True", "False" },
                1, "Never share your OTP with anyone.");

            Add("What is social engineering?",
                new[] { "Building websites", "Manipulating people to reveal information", "Updating software", "Creating backups" },
                1, "Social engineering tricks people into revealing sensitive information.");

            Add("What should you check before clicking a link?",
                new[] { "The website address", "The font colour", "The message length", "The background" },
                0, "Always inspect suspicious web addresses.");

            Add("Is it safe to share personal information publicly?",
                new[] { "True", "False" },
                1, "Oversharing can expose you to identity theft and scams.");

            Add("Why should you back up important files?",
                new[] { "To make passwords shorter", "To prevent all viruses", "To recover lost or damaged information", "To share files publicly" },
                2, "Backups help recover information after data loss or ransomware.");

        }

        private void Add(
            string question,
            string[] options,
            int correct,
            string explanation)
        {
            _questions.Add(new QuizQuestion
            {
                Text = question,
                Options = new List<string>(options),
                CorrectIndex = correct,
                Explanation = explanation
            });
        }

        public void Start()
        {
            _currentIndex = 0;
            _score = 0;
        }

        public bool SubmitAnswer(int selectedIndex)
        {
            if (selectedIndex < 0 ||
                selectedIndex >= Current.Options.Count)
                return false;

            bool correct =
                selectedIndex == Current.CorrectIndex;

            if (correct)
                _score++;

            return correct;
        }

        public void MoveNext()
        {
            if (!IsLastQuestion)
                _currentIndex++;
        }

        public string GetFinalFeedback()
        {
            if (_score == TotalQuestions)
                return "Excellent! You are a cybersecurity champion!";

            if (_score >= 9)
                return "Great job! You have strong cybersecurity knowledge.";

            if (_score >= 6)
                return "Good effort! Keep practising.";

            return "Keep learning to improve your cybersecurity awareness.";
        }
    }
}
