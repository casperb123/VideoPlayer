using NReco.VideoInfo;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;

namespace VideoPlayer.Entities
{
    public class Media
    {
        private string source;
        private string duration;

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
            get
            {
                return Path.GetFileNameWithoutExtension(source);
            }
        }

        public string Duration
        {
            get => duration;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("The duration can't be null or empty");

                duration = value;
            }
        }

        public Media(string source)
        {
            Source = source;

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

            //using (Process ffmpeg = new Process())
            //{
            //    string result;
            //    StreamReader errorReader;

            //    ffmpeg.StartInfo.UseShellExecute = false;
            //    ffmpeg.StartInfo.ErrorDialog = false;
            //    ffmpeg.StartInfo.RedirectStandardError = true;
            //    ffmpeg.StartInfo.FileName = $@"{ffmpegPath}\ffmpeg.exe";
            //    ffmpeg.StartInfo.Arguments = $"-i {Source}";

            //    ffmpeg.Start();
            //    errorReader = ffmpeg.StandardError;
            //    ffmpeg.WaitForExit(5000);

            //    result = errorReader.ReadToEnd();
            //    result = result.Substring(result.IndexOf("Duration: ") + ("Duration: ").Length, ("00:00:00.00").Length);
            //    result = result.Remove(result.IndexOf("."), 3);

            //    int colonIndex = result.IndexOf(':');
            //    double time = double.Parse(Split(result, 0, colonIndex));

            //    if (time == 0)
            //        result = result.Substring(colonIndex + 1);

            //    Duration = result;
            //}
        }

        private string Split(string source, int start, int end)
        {
            if (end < 0)
            {
                end = source.Length + end;
            }
            int len = end - start;

            return source.Substring(start, len);
        }

        private double ConvertTimeToSeconds(string time)
        {
            double totalSeconds = 0;
            int colonCount = time.Count(x => x == ':');

            if (colonCount == 1)
            {
                int index = time.IndexOf(':');

                double minutes = double.Parse(Split(time, 0, index));
                double seconds = double.Parse(time.Substring(index + 1));

                totalSeconds = (60 * minutes) + seconds;
            }
            else if (colonCount == 2)
            {
                int firstColonIndex = time.IndexOf(':');
                int secondColonIndex = time.Substring(firstColonIndex + 1).IndexOf(':');

                double hours = double.Parse(Split(time, 0, firstColonIndex));
                double minutes = double.Parse(Split(time, firstColonIndex + 1, secondColonIndex));
                double seconds = double.Parse(time.Substring(secondColonIndex + 1));

                totalSeconds = (3600 * hours) + (60 * minutes) + seconds;
            }

            return totalSeconds;
        }
    }
}
