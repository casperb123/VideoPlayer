using NReco.VideoInfo;
using System;
using System.IO;

namespace VideoPlayer.Entities
{
    [Serializable]
    public class Media
    {
        private string source;
        private string duration;
        private string name;

        public Uri Uri { get; private set; }

        public string Source
        {
            get { return source; }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new ArgumentException($"{value} isn't a valid source");
                }
                if (Uri.TryCreate(value, UriKind.Absolute, out Uri uri))
                {
                    Uri = uri;
                }
                else
                {
                    throw new ArgumentException($"{value} isn't a valid source");
                }

                source = value;
            }
        }

        public string Name
        {
            get => name;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("The name can't be null of empty");

                name = value;
            }
        }

        public string Duration
        {
            get => duration;
            private set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("The duration can't be null or empty");

                duration = value;
            }
        }

        public Media(string source)
        {
            Source = source;
            Name = Path.GetFileNameWithoutExtension(Source);

            string runningPath = AppDomain.CurrentDomain.BaseDirectory;
            string ffmpegPath = $@"{Path.GetFullPath(Path.Combine(runningPath, @"..\..\..\"))}ffmpeg";

            if (!Directory.Exists(ffmpegPath))
            {
                ffmpegPath = $@"{runningPath}\ffmpeg";
            }

            FFProbe fFProbe = new FFProbe
            {
                ToolPath = ffmpegPath
            };
            MediaInfo mediaInfo = fFProbe.GetMediaInfo(Source);
            string timeSpanString = string.Empty;

            if (mediaInfo.Duration.Hours > 0)
            {
                if (mediaInfo.Duration.Hours > 9)
                {
                    timeSpanString = @"hh\:";
                }
                else
                {
                    timeSpanString = @"h\:";
                }
            }
            if (mediaInfo.Duration.Minutes > 9 || mediaInfo.Duration.Hours > 0)
            {
                timeSpanString += @"mm\:";
            }
            else
            {
                timeSpanString += @"m\:";
            }

            Duration = mediaInfo.Duration.ToString($"{timeSpanString}ss");
        }
    }
}
