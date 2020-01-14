using DeviceId;
using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace VideoPlayer.Entities
{
    public class Settings : INotifyPropertyChanged
    {
        private int theme;
        private int color;

        public static Settings CurrentSettings = GetSettings();
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

        private void OnPropertyChanged(string prop)
        {
            if (prop != null)
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        public Settings()
        {
            Color = 1;
            Theme = 0;
        }

        public async Task Save()
        {
            string runningPath = Environment.CurrentDirectory;
            string file = $@"{runningPath}\Settings.json";
            string json = JsonConvert.SerializeObject(this, Formatting.Indented);

            await File.WriteAllTextAsync(file, json);
        }

        private static Settings GetSettings()
        {
            string runningPath = Environment.CurrentDirectory;
            string settingsFile = $@"{runningPath}\Settings.json";

            if (!File.Exists(settingsFile))
                return new Settings();

            return JsonConvert.DeserializeObject<Settings>(File.ReadAllText(settingsFile));
        }
    }
}
