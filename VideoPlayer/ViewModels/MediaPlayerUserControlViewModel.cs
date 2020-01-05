using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using VideoPlayer.Entities;
using VideoPlayer.UserControls;
using MaterialDesignThemes.Wpf;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using VideoPlayer.Windows;
using System.Collections.Generic;
using MahApps.Metro.Controls;
using System.Windows.Controls;

namespace VideoPlayer.ViewModels
{
    public class MediaPlayerUserControlViewModel : INotifyPropertyChanged
    {
        private readonly MediaPlayerUserControl userControl;
        private double loopStart;
        private double loopEnd;
        private ObservableCollection<Media> queue;
        private ObservableCollection<Media> oldQueue;
        private Media selectedMedia;

        public bool DarkTheme;
        public bool Seeking;
        public TimeSpan position;
        public double OldVolume;
        public bool LoopVideo;
        public bool LoopSpecificTime;
        public DispatcherTimer ProgressTimer;

        public event PropertyChangedEventHandler PropertyChanged;

        public Media SelectedMedia
        {
            get => selectedMedia;
            set
            {
                if (value is null)
                    throw new ArgumentNullException("The selected media can't be null");

                selectedMedia = value;
            }
        }

        public ObservableCollection<Media> Queue
        {
            get => queue;
            set
            {
                if (value is null)
                    throw new ArgumentNullException("The queue can't be null");

                queue = value;
                OnPropertyChanged(nameof(Queue));
            }
        }

        public ObservableCollection<Media> OldQueue
        {
            get => oldQueue;
            set
            {
                if (value is null)
                    throw new ArgumentNullException("The old queue can't be null");

                oldQueue = value;
            }
        }

        private void OnPropertyChanged(string prop)
        {
            if (prop != null)
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }

        public MediaPlayerUserControlViewModel(MediaPlayerUserControl mediaPlayerUserControl, List<Media> medias)
            : this(mediaPlayerUserControl)
        {
            AddMediasToQueue(medias).ConfigureAwait(false);
        }

        public MediaPlayerUserControlViewModel(MediaPlayerUserControl mediaPlayerUserControl)
        {
            userControl = mediaPlayerUserControl;

            userControl.Focusable = true;
            userControl.Loaded += (s, e) => Keyboard.Focus(userControl);

            ProgressTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(300)
            };
            ProgressTimer.Tick += ProgressTimer_Tick;

            queue = new ObservableCollection<Media>();
            oldQueue = new ObservableCollection<Media>();
        }

        private async void ProgressTimer_Tick(object sender, EventArgs e)
        {
            if (userControl.player.IsPlaying)
            {
                if (LoopSpecificTime && loopEnd > loopStart)
                {
                    if (userControl.sliderProgress.Value < loopStart ||
                        userControl.sliderProgress.Value >= loopEnd)
                    {
                        ProgressTimer.Stop();
                        Seeking = true;

                        int pos = Convert.ToInt32(loopStart);
                        await userControl.player.Pause();
                        await Seek(new TimeSpan(0, 0, 0, pos, 0));
                    }
                }

                if (!Seeking)
                    userControl.sliderProgress.Value = userControl.player.Position.TotalSeconds;
            }
        }

        public async Task Seek(TimeSpan timeSpan)
        {
            await userControl.player.Seek(timeSpan);
            Seeking = false;
            ProgressTimer.Start();
            await userControl.player.Play();
        }

        public void ShowTime()
        {
            if (userControl.player.IsOpen)
            {
                TimeSpan currentTime = TimeSpan.FromSeconds(userControl.sliderProgress.Value);
                userControl.textBlockDuration.Text = $"{currentTime.ToString(@"m\:ss")} / {userControl.player.NaturalDuration.Value.ToString(@"m\:ss")}";
            }
        }

        public void DisablePlayPause()
        {
            userControl.buttonPlayPause.IsEnabled = false;
            userControl.iconPlayPause.Kind = PackIconKind.Play;
        }

        public void EnablePlayPause()
        {
            userControl.buttonPlayPause.IsEnabled = true;
        }

        public void DisableStop()
        {
            userControl.buttonStop.IsEnabled = false;
        }

        public void EnableStop()
        {
            userControl.buttonStop.IsEnabled = true;
        }

        public async Task AddToQueue(Media media)
        {
            if (userControl.player.IsOpen || Queue.Count >= 1)
            {
                Queue.Add(media);
                userControl.buttonSkipForward.IsEnabled = true;
            }
            else
            {
                await Open(media);
                SelectedMedia = media;
            }
        }

        public async Task Open(Media media)
        {
            if (userControl.player.IsOpen)
            {
                await userControl.player.Close();
                ResetLoop();
                ResetProgress();
            }

            await userControl.player.Open(media.Uri);
            await Play();
        }

        public async Task Play()
        {
            ProgressTimer.Start();
            await userControl.player.Play();
            userControl.iconPlayPause.Kind = PackIconKind.Pause;
            EnablePlayPause();
            EnableStop();
        }

        public async Task Pause()
        {
            ProgressTimer.Stop();
            await userControl.player.Pause();
            userControl.iconPlayPause.Kind = PackIconKind.Play;
        }

        public async Task Stop(bool resetSource = true)
        {
            userControl.iconPlayPause.Kind = PackIconKind.Play;
            DisableStop();

            ProgressTimer.Stop();
            ResetProgress();
            if (resetSource)
            {
                await userControl.player.Close();
                userControl.player.Source = null;
                ResetControls();
            }
            else
            {
                await userControl.player.Stop();
            }
        }

        public void ResetControls()
        {
            userControl.textBoxLoopStart.IsEnabled = false;
            userControl.textBoxLoopEnd.IsEnabled = false;
            ResetLoop();
            userControl.checkBoxLoopTime.IsEnabled = false;
            userControl.checkBoxLoopTime.IsChecked = false;
            userControl.sliderProgress.IsEnabled = false;
        }

        public void ResetProgress()
        {
            userControl.sliderProgress.Value = 0;
            userControl.textBlockDuration.Text = "0:00 / 0:00";
        }

        public bool MuteToggle()
        {
            if (userControl.player.Volume > 0)
            {
                OldVolume = userControl.player.Volume;
                userControl.sliderVolume.Value = 0;

                return true;
            }
            else if (OldVolume == 0)
            {
                userControl.sliderVolume.Value = 1;
                OldVolume = userControl.player.Volume;
            }
            else
            {
                userControl.sliderVolume.Value = OldVolume;
                OldVolume = userControl.player.Volume;
            }

            return false;
        }

        public void ToggleSettings()
        {
            userControl.gridSettings.IsEnabled = !userControl.gridSettings.IsEnabled;

            if (userControl.gridSettings.IsEnabled)
            {
                userControl.gridSettings.Visibility = Visibility.Visible;
                userControl.gridSettings.Focus();
            }
            else
            {
                userControl.gridSettings.Visibility = Visibility.Hidden;
            }
        }

        public void ChangeSpeed(double speed)
        {
            userControl.player.SpeedRatio = speed;
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

        private string ConvertSecondsToTime(double seconds)
        {
            TimeSpan timeSpan = TimeSpan.FromSeconds(seconds);
            string timeSpanString = string.Empty;

            if (timeSpan.Hours > 0)
            {
                if (timeSpan.Hours > 9)
                {
                    timeSpanString = @"hh\:";
                }
                else
                {
                    timeSpanString = @"h\:";
                }
            }
            if (timeSpan.Minutes > 9 || timeSpan.Hours > 0)
            {
                timeSpanString += @"mm\:";
            }
            else
            {
                timeSpanString += @"m\:";
            }

            return timeSpan.ToString($@"{timeSpanString}ss");
        }

        public void SetLoopTime(string start, string end)
        {
            Regex firstSyntax = new Regex("[0-9]:[0-59]");
            Regex secondSyntax = new Regex("[0-59]:[0-59]");
            Regex thirdSyntax = new Regex("[0-9]:[0-59]:[0-59]");

            if (userControl.player.IsOpen &&
                firstSyntax.IsMatch(start) && firstSyntax.IsMatch(end) ||
                secondSyntax.IsMatch(start) && secondSyntax.IsMatch(end) ||
                thirdSyntax.IsMatch(start) && thirdSyntax.IsMatch(end))
            {
                double startSeconds = ConvertTimeToSeconds(start);
                double endSeconds = ConvertTimeToSeconds(end);

                loopStart = startSeconds;
                loopEnd = endSeconds;

                if (loopStart > 0 && loopEnd > 0 &&
                    loopStart < loopEnd)
                {
                    SetSelection(loopStart, loopEnd);
                    userControl.hyperLinkResetLoop.IsEnabled = true;
                }
                else
                {
                    userControl.hyperLinkResetLoop.IsEnabled = false;
                }
            }
            else
            {
                loopEnd = 0;
                loopStart = 0;
                userControl.hyperLinkResetLoop.IsEnabled = false;
            }
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

        public double PixelsToValue(double pixels, double minValue, double maxValue, double width)
        {
            double range = maxValue - minValue;
            double percentage = (pixels / width) * 100;
            return (percentage / 100 * range) + minValue;
        }

        public void SetLoopValue(Point point)
        {
            double value = PixelsToValue(point.X, userControl.sliderProgress.Minimum, userControl.sliderProgress.Maximum, userControl.sliderProgress.ActualWidth);

            if (loopStart == 0 && loopEnd == 0 ||
                Math.Abs(loopStart - value) < Math.Abs(loopEnd - value) && loopEnd > 0)
            {
                userControl.textBoxLoopStart.Text = ConvertSecondsToTime(value);
            }
            else if (loopEnd == 0)
            {
                userControl.textBoxLoopEnd.Text = ConvertSecondsToTime(value);
                userControl.checkBoxLoopTime.IsChecked = true;
            }
            else
            {
                userControl.textBoxLoopEnd.Text = ConvertSecondsToTime(value);
            }
        }

        public void SetSelection(double start, double end)
        {
            userControl.sliderProgress.SelectionStart = start;
            userControl.sliderProgress.SelectionEnd = end;
        }

        public void ResetLoop()
        {
            userControl.textBoxLoopStart.Text = "0:00";
            userControl.textBoxLoopEnd.Text = "0:00";
            userControl.checkBoxLoopTime.IsChecked = false;
        }

        public async Task PreviousTrack()
        {
            Queue.Insert(0, SelectedMedia);
            SelectedMedia = OldQueue[OldQueue.Count - 1];
            OldQueue.Remove(SelectedMedia);
            await Open(SelectedMedia);

            if (OldQueue.Count == 0)
            {
                userControl.buttonSkipBackwards.IsEnabled = false;
            }
            else
            {
                userControl.buttonSkipBackwards.IsEnabled = true;
            }

            userControl.buttonSkipForward.IsEnabled = true;
        }

        public void ToggleQueuePanel()
        {
            userControl.flyoutQueue.IsOpen = !userControl.flyoutQueue.IsOpen;
        }

        public async Task ChangeTrack(int index)
        {
            Media media = Queue[index];
            await Open(media);

            if (index + 1 == Queue.Count)
            {
                userControl.buttonSkipForward.IsEnabled = false;
            }
            else
            {
                userControl.buttonSkipForward.IsEnabled = true;
            }

            List<Media> oldMedias = Queue.Where(x => Queue.IndexOf(x) < index).ToList();
            List<Media> medias = Queue.Where(x => Queue.IndexOf(x) > index).ToList();
            Queue = new ObservableCollection<Media>(medias);

            if (SelectedMedia != null)
                OldQueue.Add(SelectedMedia);

            foreach (Media oldMedia in oldMedias)
            {
                OldQueue.Add(oldMedia);
            }

            SelectedMedia = media;

            if (OldQueue.Count > 0)
            {
                userControl.buttonSkipBackwards.IsEnabled = true;
            }
            else
            {
                userControl.buttonSkipBackwards.IsEnabled = false;
            }
        }

        public async Task AddMediasToQueue(List<Media> medias)
        {
            foreach (Media media in medias)
            {
                await AddToQueue(media);
            }
        }
    }
}
