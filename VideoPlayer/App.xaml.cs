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
                MessageBox.Show("Please don't open the application with another file", "Video Player", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string runningPath = AppDomain.CurrentDomain.BaseDirectory;
#if DEBUG
            string ffmpegPath = $@"{Path.GetFullPath(Path.Combine(runningPath, @"..\..\..\"))}ffmpeg";
#else
            string ffmpegPath = $@"{runningPath}\ffmpeg";
#endif

            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string settingsPath = $@"{appDataPath}\VideoPlayer";
            Settings.MediasPath = $@"{settingsPath}\Medias";
            Settings.SettingsFilePath = $@"{settingsPath}\Settings.json";
            Settings.PlaylistsFilePath = $@"{settingsPath}\Playlists.bin";

            if (!Directory.Exists(settingsPath))
                Directory.CreateDirectory(settingsPath);
            if (!Directory.Exists(Settings.MediasPath))
                Directory.CreateDirectory(Settings.MediasPath);

            Unosquare.FFME.Library.FFmpegDirectory = ffmpegPath;
            Settings.CurrentSettings = Settings.GetSettings().Result;
            base.OnStartup(e);
        }
    }
}
