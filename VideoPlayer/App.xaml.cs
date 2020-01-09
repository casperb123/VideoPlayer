using System;
using System.IO;
using System.Windows;

namespace VideoPlayer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            string runningPath = AppDomain.CurrentDomain.BaseDirectory;
            string ffmpegPath = $@"{Path.GetFullPath(Path.Combine(runningPath, @"..\..\..\"))}ffmpeg";

            if (!Directory.Exists(ffmpegPath))
            {
                ffmpegPath = $@"{runningPath}\ffmpeg";
            }

            Unosquare.FFME.Library.FFmpegDirectory = ffmpegPath;
            base.OnStartup(e);
        }
    }
}
