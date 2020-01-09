﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;

namespace VideoPlayer.Entities
{
    [Serializable]
    public class Playlist : Protected, INotifyPropertyChanged
    {
        private string name;
        private ObservableCollection<Media> medias;
        private int mediaCount;
        private string nameAndCount;
        private int index;

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

        public string NameAndCount
        {
            get => nameAndCount;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("The value can't be null of empty");

                nameAndCount = value;
                OnPropertyChanged(nameof(NameAndCount));
            }
        }

        public int MediaCount
        {
            get => mediaCount;
            set
            {
                mediaCount = value;
                OnPropertyChanged(nameof(MediaCount));
            }
        }

        public int Index
        {
            get => index;
            set
            {
                if (value < 0)
                    throw new ArgumentException("The index can't be lower than 0");

                index = value;
                OnPropertyChanged(nameof(Index));
            }
        }

        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string prop)
        {
            if (!string.IsNullOrWhiteSpace(prop))
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        public Playlist(string name, ICollection<Media> medias)
            : this(name)
        {
            medias.ToList().ForEach(x => Medias.Add(x));
            NameAndCount = $"{Name} ({Medias.Count})";
            MediaCount = Medias.Count;
        }

        public Playlist(string name)
        {
            Name = name;
            medias = new ObservableCollection<Media>();
            NameAndCount = $"{Name} ({Medias.Count})";
            MediaCount = Medias.Count;
        }

        public async Task<(bool isValid, string message)> Save(bool update = false)
        {
            string runningPath = AppDomain.CurrentDomain.BaseDirectory;
            string playlistsPath = $@"{runningPath}\Playlists";
            string file = $@"{playlistsPath}\{Name}.playlist";

            if (File.Exists(file) && update == false)
                return (false, $"A playlist with the name '{Name}' already exists");

            BinaryFormatter formatter = new BinaryFormatter();
            MemoryStream stream = new MemoryStream();
            formatter.Serialize(stream, this);
            await File.WriteAllBytesAsync(file, Protect(stream.ToArray()));
            return (true, null);
            //string[] playlistPaths = Medias.ToList().Select(x => x.Source).ToArray();
            //BinaryFormatter formatter = new BinaryFormatter();
            //MemoryStream stream = new MemoryStream();
            //formatter.Serialize(stream, Medias);
            //await File.WriteAllBytesAsync($@"{playlistsPath}\{Name}.playlist", Protect(stream.ToArray()));
            //return (true, null);
        }

        public void UpdateMediaCount()
        {
            MediaCount = Medias.Count;
            NameAndCount = $"{Name} ({Medias.Count})";
        }
    }
}
