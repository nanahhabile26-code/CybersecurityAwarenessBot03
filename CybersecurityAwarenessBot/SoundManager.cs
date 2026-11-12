
using System;
using System.IO;
using System.Media;
using System.Windows;

namespace CybersecurityAwarenessBot.Services
{
    public static class SoundManager
    {
        private static SoundPlayer? _welcomePlayer;

        public static void PlayWelcomeSound()
        {
            try
            {
                string soundPath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "Assets",
                    "Welcome.wav"
                );

                if (!File.Exists(soundPath))
                {
                    MessageBox.Show(
                        "Welcome.wav was not found!\n" + soundPath,
                        "Sound Error"
                    );

                    return;
                }

                _welcomePlayer = new SoundPlayer(soundPath);

                _welcomePlayer.Load();
                _welcomePlayer.Play();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Unable to play welcome sound:\n" + ex.Message,
                    "Sound Error"
                );
            }
        }
    }
}
