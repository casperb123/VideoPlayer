using MahApps.Metro.Controls;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using VideoPlayer.UserControls;
using VideoPlayer.Windows;

namespace VideoPlayer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        private readonly string[] validExtensions = new string[]
        {
            ".mpg",
            ".avi",
            ".wma",
            ".mov",
            ".wav",
            ".mp2",
            ".mp3",
            ".mp4"
        };

        public MainWindow()
        {
            InitializeComponent();
            WindowSettings windowSettings = new WindowSettings(true);

            string[] cmdLine = Environment.GetCommandLineArgs();
            if (cmdLine.Length == 2 && validExtensions.Contains(Path.GetExtension(cmdLine[1])))
            {
                masterUserControl.Content = new MediaPlayerUserControl(cmdLine[1]);
            }
            else
            {
                masterUserControl.Content = new MediaPlayerUserControl();
            }
        }

        private void ButtonWindowSettings_Click(object sender, RoutedEventArgs e)
        {
            WindowSettings windowSettings = new WindowSettings();
            windowSettings.Owner = this;
            windowSettings.ShowDialog();
        }

        private void ButtonWindowCredits_Click(object sender, RoutedEventArgs e)
        {
            Credits credits = new Credits();
            credits.Owner = this;
            credits.ShowDialog();
        }
    }
}
