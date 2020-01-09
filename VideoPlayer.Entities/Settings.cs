using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace VideoPlayer.Entities
{
    public class Settings : Protected, INotifyPropertyChanged
    {
		private int theme;
		private int color;
		private byte[] salt;

		public event PropertyChangedEventHandler PropertyChanged;

		public byte[] Salt
		{
			get => salt;
			set
			{
				if (value is null || value.Length <= 0)
					throw new NullReferenceException("The salt can't be null or empty");

				salt = value;
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

		private void OnPropertyChanged(string prop)
		{
			if (prop != null)
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
		}

		public Settings()
		{
			Color = 1;
			Theme = 0;
			Salt = Protect(Guid.NewGuid().ToByteArray());
			Save().ConfigureAwait(false);
		}

		public async Task Save()
		{
			string runningPath = AppDomain.CurrentDomain.BaseDirectory;
			string file = $@"{runningPath}\Settings.json";
			string json = JsonConvert.SerializeObject(this, Formatting.Indented);

			await File.WriteAllTextAsync(file, json);
		}
	}
}
