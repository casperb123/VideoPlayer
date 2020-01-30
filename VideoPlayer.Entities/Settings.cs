using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace VideoPlayer.Entities
{
    public class Settings : INotifyPropertyChanged
    {
        private int theme;
        private int color;

        public static Settings CurrentSettings;
        public event PropertyChangedEventHandler PropertyChanged;

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

        public Settings()
        {
            Color = 1;
            NotifyUpdates = true;
        }

        private void OnPropertyChanged(string prop)
        {
            if (prop != null)
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        public async Task Save()
        {
            string runningPath = GetCurrentDir();
            string file = $@"{runningPath}\Settings.json";
            string json = JsonConvert.SerializeObject(this, Formatting.Indented);

            await File.WriteAllTextAsync(file, json);
        }

        public static async Task<Settings> GetSettings()
        {
            string runningPath = GetCurrentDir();
            string settingsFile = $@"{runningPath}\Settings.json";

            if (!File.Exists(settingsFile))
            {
                Settings newSettings = new Settings();
                await newSettings.Save();
                return newSettings;
            }

            JObject obj = JsonConvert.DeserializeObject<dynamic>(File.ReadAllText(settingsFile));
            List<JProperty> removedProperties = obj.Properties().Where(x => typeof(Settings).GetProperty(x.Name) is null).ToList();

            if (removedProperties != null && removedProperties.Count > 0)
                removedProperties.ForEach(x => obj.Remove(x.Name));

            bool missingProperties = typeof(Settings).GetProperties().Any(x => obj.Property(x.Name) is null);
            Settings settings = obj.ToObject<Settings>();

            if (missingProperties)
                await settings.Save();

            return settings;
        }

        public static string GetCurrentDir()
        {
            return AppDomain.CurrentDomain.BaseDirectory;
        }
    }
}
