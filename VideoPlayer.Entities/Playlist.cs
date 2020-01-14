using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Collections.Specialized;

namespace VideoPlayer.Entities
{
    [Serializable]
    public class Playlist : INotifyPropertyChanged
    {
        private string name;
        private ObservableCollection<Media> medias;
        private string nameAndCount;

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
                NameAndCount = $"{Name} ({Medias.Count})";
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
        }

        public Playlist(string name)
        {
            medias = new ObservableCollection<Media>();
            Medias.CollectionChanged += Medias_CollectionChanged;
            Name = name;
        }

        private void Medias_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(Medias.Count));
            NameAndCount = $"{Name} ({Medias.Count})";
        }
    }
}
