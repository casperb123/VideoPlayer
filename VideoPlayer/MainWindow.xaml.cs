using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using VideoPlayer.Entities;
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
            if (cmdLine.Length >= 2)
            {
                List<string> filePaths = cmdLine.Where(x => validExtensions.Contains(Path.GetExtension(x))).ToList();
                List<Media> medias = new List<Media>();
                filePaths.ForEach(x => medias.Add(new Media(x)));

                masterUserControl.Content = new MediaPlayerUserControl(medias);
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
