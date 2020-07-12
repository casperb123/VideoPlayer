using Newtonsoft.Json.Bson;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using VideoPlayer.Entities;

namespace VideoPlayer.ViewModels
{
    public sealed class SoundProcessorViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly SoundProcessor processor;

        private int tempo;
        private int pitch;
        private int rate;
        private string? fileName;

        public SoundProcessorViewModel()
        {
            processor = new SoundProcessor();
        }

        public event PropertyChangedEventHandler PropertyChanged = (sender, e) => { };

        private enum PlaybackMode
        {
            Unloaded,
            Stopped,
            Playing,
            Paused
        }

        public int Tempo
        {
            get => tempo;
            set
            {
                Set(ref tempo, value);

                if (processor.ProcessorStream != null)
                    processor.ProcessorStream.TempoChange = value;
            }
        }

        public int Pitch
        {
            get => pitch;
            set
            {
                Set(ref pitch, value);

                if (processor.ProcessorStream != null)
                    processor.ProcessorStream.PitchSemiTones = value;
            }
        }
        
        public int Rate
        {
            get => rate;
            set
            {
                Set(ref rate, value);

                if (processor.ProcessorStream != null)
                    processor.ProcessorStream.RateChange = value;
            }
        }

        public double Volume
        {
            get => processor.Volume;
            set
            {
                processor.Volume = value;
            }
        }

        public string? FileName
        {
            get => fileName;
            set => Set(ref fileName, value);
        }

        public void Dispose()
        {
            processor.Dispose();
        }

        public void OpenFile(Media media)
        {
            Stop();

            if (processor.OpenFile(media.Source))
                FileName = media.Source;
            else
                FileName = string.Empty;
        }

        public void Play()
        {
            processor.Play();
        }

        public void Pause()
        {
            processor.Pause();
        }

        public void Stop()
        {
            processor.Stop();
        }

        public void Seek(TimeSpan timeSpan)
        {
            processor.ProcessorStream.CurrentTime = timeSpan;
        }

        public void Set<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(storage, value))
                return;

            storage = value;
            OnPropertyChanged(propertyName);
        }

        public void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
    }
}
