using System;
using System.IO;
using System.Reflection;
using System.Windows;
using VideoPlayer.Entities;

namespace VideoPlayer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            if (e.Args.Length > 0)
            {
                MessageBox.Show("Please don't open the application with another file", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string runningPath = Directory.GetCurrentDirectory();
#if DEBUG
            string ffmpegPath = $@"{Path.GetFullPath(Path.Combine(runningPath, @"..\..\..\"))}ffmpeg";
#else
            string ffmpegPath = $@"{runningPath}\ffmpeg";
#endif

            Unosquare.FFME.Library.FFmpegDirectory = ffmpegPath;
            Settings.CurrentSettings = Settings.GetSettings().Result;
            base.OnStartup(e);
        }
    }
}
