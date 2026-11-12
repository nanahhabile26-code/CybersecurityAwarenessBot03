
using System.Windows;
using System.Windows.Controls;
using CybersecurityAwarenessBot.Services;

namespace CybersecurityAwarenessBot
{
    public partial class QuizWindow : Window
    {
        private readonly QuizService _quizService;
        private bool _answered;

        public QuizWindow()
        {
            InitializeComponent();

            _quizService = new QuizService();
            _quizService.Start();

            DisplayQuestion();
        }

        private void DisplayQuestion()
        {
            _answered = false;

            QuestionText.Text = _quizService.Current.Text;

            ProgressText.Text =
                "Question " + _quizService.CurrentNumber +
                " of " + _quizService.TotalQuestions;

            ScoreText.Text = "Score: " + _quizService.Score;

            OptionsPanel.Children.Clear();

            foreach (string option in _quizService.Current.Options)
            {
                RadioButton radio = new RadioButton
                {
                    Content = option,
                    Margin = new Thickness(5, 8, 5, 8),
                    FontSize = 15,
                    Foreground = System.Windows.Media.Brushes.White,
                    GroupName = "QuizOptions"
                };

                OptionsPanel.Children.Add(radio);
            }

            FeedbackText.Text = "";

            NextButton.Content = "Submit Answer";
            NextButton.Visibility = Visibility.Visible;
            NextButton.IsEnabled = true;
        }

        private void NextButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            // First click: submit the selected answer.
            if (!_answered)
            {
                RadioButton? selectedOption = null;

                foreach (RadioButton option in OptionsPanel.Children)
                {
                    if (option.IsChecked == true)
                    {
                        selectedOption = option;
                        break;
                    }
                }

                if (selectedOption == null)
                {
                    MessageBox.Show(
                        "Please select an answer before continuing.",
                        "No Answer Selected",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    return;
                }

                int selectedIndex =
                    OptionsPanel.Children.IndexOf(selectedOption);

                bool correct =
                    _quizService.SubmitAnswer(selectedIndex);

                if (correct)
                {
                    FeedbackText.Text =
                        "Correct! " + _quizService.Current.Explanation;
                }
                else
                {
                    FeedbackText.Text =
                        "Incorrect. " + _quizService.Current.Explanation;
                }

                ScoreText.Text = "Score: " + _quizService.Score;

                // Prevent changing the answer after submission.
                foreach (RadioButton option in OptionsPanel.Children)
                {
                    option.IsEnabled = false;
                }

                _answered = true;

                NextButton.Content = _quizService.IsLastQuestion
                    ? "View Results"
                    : "Next Question";

                return;
            }

            // Second click: move to the next question or show results.
            if (_quizService.IsLastQuestion)
            {
                ShowResults();
            }
            else
            {
                _quizService.MoveNext();
                DisplayQuestion();
            }
        }

        private void ShowResults()
        {
            QuestionPanel.Visibility = Visibility.Collapsed;
            ResultPanel.Visibility = Visibility.Visible;

            FinalScoreText.Text =
                "Your final score: " +
                _quizService.Score + " / " +
                _quizService.TotalQuestions;

            FinalFeedbackText.Text =
                _quizService.GetFinalFeedback();
        }

        private void PlayAgain_Click(
            object sender,
            RoutedEventArgs e)
        {
            _quizService.Start();

            QuestionPanel.Visibility = Visibility.Visible;
            ResultPanel.Visibility = Visibility.Collapsed;

            DisplayQuestion();
        }

        private void Close_Click(
            object sender,
            RoutedEventArgs e)
        {
            Close();
        }
    }
}
