using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace VideoPlayer.Entities
{
    public class Settings : INotifyPropertyChanged
    {
        private int theme;
        private int color;
        private bool leftRightedgeDetection;
        private int leftRightedgeDistance;
        private bool alwaysOnTop;
        private bool topEdgeDetection;
        private double volume;

        public static Settings CurrentSettings;
        public static string SettingsFilePath;
        public static string PlaylistsFilePath;
        public static string MediasPath;

        public event PropertyChangedEventHandler PropertyChanged;

        public bool AlwaysOnTop
        {
            get => alwaysOnTop;
            set
            {
                alwaysOnTop = value;
                OnPropertyChanged(nameof(AlwaysOnTop));

                if (Application.Current.MainWindow != null && Application.Current.MainWindow.IsLoaded)
                    Application.Current.MainWindow.Topmost = value;
            }
        }

        public int Color
        {
            get => color;
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(Color), "The color can't be lower than 0");

                color = value;
                OnPropertyChanged(nameof(Color));
            }
        }

        public int Theme
        {
            get => theme;
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(Theme), "The theme can't be lower than 0");

                theme = value;
                OnPropertyChanged(nameof(Theme));
            }
        }

        public bool CheckForUpdates { get; set; }

        public bool NotifyUpdates { get; set; }

        public bool LeftRightEdgeDetection
        {
            get => leftRightedgeDetection;
            set
            {
                leftRightedgeDetection = value;
                OnPropertyChanged(nameof(LeftRightEdgeDetection));
            }
        }

        public enum EdgeOpen
        {
            Queue,
            Playlists,
            Settings
        }

        public EdgeOpen RightEdgeOpen { get; set; }

        public EdgeOpen LeftEdgeOpen { get; set; }

        public int LeftRightEdgeDistance
        {
            get => leftRightedgeDistance;
            set
            {
                if (value < 5 || value > 200)
                    throw new ArgumentOutOfRangeException(nameof(LeftRightEdgeDistance), "The left/right edge distance must be between 5 and 200");

                leftRightedgeDistance = value;
            }
        }

        public bool TopEdgeDetection
        {
            get => topEdgeDetection;
            set
            {
                topEdgeDetection = value;
                OnPropertyChanged(nameof(TopEdgeDetection));
            }
        }

        public double Volume
        {
            get => volume;
            set
            {
                if (value < 0 || volume > 1)
                    throw new ArgumentOutOfRangeException(nameof(Volume), "The volume must be between 0 and 1");

                volume = value;
                OnPropertyChanged(nameof(Volume));
            }
        }

        public Settings()
        {
            Color = 1;
            NotifyUpdates = true;
            CheckForUpdates = true;
            RightEdgeOpen = EdgeOpen.Queue;
            LeftEdgeOpen = EdgeOpen.Settings;
            LeftRightEdgeDetection = true;
            LeftRightEdgeDistance = 50;
            TopEdgeDetection = true;
            Volume = 1;
        }

        private void OnPropertyChanged(string prop)
        {
            if (prop != null)
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        public async Task Save()
        {
            string json = JsonConvert.SerializeObject(this, Formatting.Indented);
            await File.WriteAllTextAsync(SettingsFilePath, json);
        }

        public static async Task<Settings> GetSettings()
        {
            if (!File.Exists(SettingsFilePath))
            {
                Settings newSettings = new Settings();
                await newSettings.Save();
                return newSettings;
            }

            JObject obj = JsonConvert.DeserializeObject<dynamic>(File.ReadAllText(SettingsFilePath));
            List<JProperty> removedProperties = obj.Properties().Where(x => typeof(Settings).GetProperty(x.Name) is null).ToList();

            if (removedProperties != null && removedProperties.Count > 0)
                removedProperties.ForEach(x => obj.Remove(x.Name));

            bool missingProperties = typeof(Settings).GetProperties().Any(x => obj.Property(x.Name) is null);
            Settings settings = obj.ToObject<Settings>();

            if (missingProperties)
                await settings.Save();

            return settings;
        }
    }
}
