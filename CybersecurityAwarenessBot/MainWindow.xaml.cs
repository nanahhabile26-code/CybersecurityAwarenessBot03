
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using CybersecurityAwarenessBot.Services;

namespace CybersecurityAwarenessBot
{
    public partial class MainWindow : Window
    {
        private readonly ChatEngine _engine;
        private readonly ActivityLogService _activityLog;
        private TaskService? _taskService;

        public MainWindow()
        {
            InitializeComponent();

            // Play welcome sound when the window has loaded.
            Loaded += (sender, e) =>
            {
                SoundManager.PlayWelcomeSound();
            };

            _engine = new ChatEngine();
            _activityLog = new ActivityLogService();

            // Load the Part 1 logo.
            AsciiArt.Text = AsciiLogo.Text;

            AddBotMessage(
                "Welcome to the Cybersecurity Awareness Bot!\n" +
                "Ask me about passwords, phishing, scams, privacy, " +
                "malware or safe browsing.");

            _activityLog.Log("System", "Chatbot started.");

            // Initialise MySQL without preventing the chatbot from opening
            // if the database is unavailable.
            try
            {
                DatabaseService database = new DatabaseService();
                database.CreateDatabaseAndTable();

                _taskService = new TaskService(database);

                _activityLog.Log(
                    "Database",
                    "Database connected successfully.");
            }
            catch (Exception ex)
            {
                _taskService = null;

                _activityLog.Log(
                    "Database",
                    "Database connection failed: " + ex.Message);
            }
        }

        private void AddBotMessage(string message)
        {
            TextBlock text = new TextBlock
            {
                Text = "Bot: " + message,
                Foreground = Brushes.White,
                FontSize = 14,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(10)
            };

            ChatPanel.Children.Add(text);
            ChatScroll.ScrollToEnd();
        }

        private void AddUserMessage(string message)
        {
            TextBlock text = new TextBlock
            {
                Text = "You: " + message,
                Foreground = Brushes.LightBlue,
                FontSize = 14,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(10)
            };

            ChatPanel.Children.Add(text);
            ChatScroll.ScrollToEnd();
        }

        private void ProcessMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;

            AddUserMessage(message);

            string lowerMessage = message.ToLowerInvariant();

            if (lowerMessage.Contains("show activity log") ||
                lowerMessage.Contains("what have you done for me"))
            {
                AddBotMessage(_activityLog.FormatForChat(10, 0));
                return;
            }

            if (lowerMessage.Contains("show more"))
            {
                AddBotMessage(_activityLog.FormatForChat(10, 10));
                return;
            }

            if (ChatEngine.IsExit(message))
            {
                _activityLog.Log("System", "User exited the chatbot.");
                AddBotMessage("Goodbye! Stay safe online.");
                return;
            }

            string response = _engine.GetResponse(message);

            _activityLog.Log("Chat", "User asked: " + message);

            AddBotMessage(response);
        }

        private void SendButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            string message = InputBox.Text.Trim();

            if (message.Length == 0)
                return;

            InputBox.Clear();
            ProcessMessage(message);
        }

        private void InputBox_KeyDown(
            object sender,
            KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SendButton_Click(sender, e);
                e.Handled = true;
            }
        }

        private void TopicButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                string topic = button.Tag?.ToString()
                    ?? button.Content?.ToString()
                    ?? "cybersecurity";

                if (topic.Equals(
                    "exit",
                    StringComparison.OrdinalIgnoreCase))
                {
                    Close();
                    return;
                }

                ProcessMessage(topic);
            }
        }

        private void TaskButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (_taskService == null)
            {
                MessageBox.Show(
                    "The task database is unavailable. " +
                    "Please check your MySQL connection and password.",
                    "Database Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            TaskWindow window =
                new TaskWindow(_taskService, _activityLog);

            window.Owner = this;
            window.ShowDialog();
        }

        private void QuizButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            QuizWindow window = new QuizWindow();
            window.Owner = this;
            window.ShowDialog();

            _activityLog.Log(
                "Quiz",
                "Cybersecurity quiz window opened.");
        }

        private void ActivityLogButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            AddBotMessage(
                _activityLog.FormatForChat(10, 0));
        }
    }
}
