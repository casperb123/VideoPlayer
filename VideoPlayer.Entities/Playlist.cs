using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Linq;

namespace VideoPlayer.Entities
{
    public class Playlist : INotifyPropertyChanged
    {
        private string name;
        private ObservableCollection<Media> medias;

        public ObservableCollection<Media> Medias
        {
            get { return medias; }
            set
            {
                if (value is null || value.Count < 1)
                    throw new NullReferenceException("The medias can't be null or whitespace");

                medias = value;
                OnPropertyChanged(nameof(Medias));
            }
        }

        public string Name
        {
            get { return name; }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("The name can't be null or whitespace");

                name = value;
                OnPropertyChanged(nameof(Name));
            }
        }

        public string NameAndMedias
        {
            get
            {
                return $"{Name} ({Medias.Count})";
            }
        }

        public int MediaCount
        {
            get
            {
                return Medias.Count;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string prop)
        {
            if (!string.IsNullOrWhiteSpace(prop))
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        public Playlist(ICollection<Media> medias)
        {
            this.medias = new ObservableCollection<Media>(medias);
        }

        public Playlist(ICollection<Media> medias, string name)
            : this(medias)
        {
            Name = name;
        }

        public async Task<(bool isValid, string message)> Save()
        {
            string runningPath = AppDomain.CurrentDomain.BaseDirectory;
            string playlistsPath = $@"{runningPath}\Playlists";
            string file = $@"{playlistsPath}\{Name}.playlist";

            if (File.Exists(file))
                return (false, "A playlist with that name already exists");

            string[] playlistPaths = Medias.ToList().Select(x => x.Source).ToArray();
            await File.WriteAllLinesAsync($@"{playlistsPath}\{Name}.playlist", playlistPaths);
            return (true, null);
        }
    }
}
