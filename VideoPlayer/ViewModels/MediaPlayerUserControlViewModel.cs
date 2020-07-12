using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using VideoPlayer.Entities;
using VideoPlayer.UserControls;
using MaterialDesignThemes.Wpf;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Controls;
using System.ComponentModel;
using Microsoft.Win32;
using System.Threading;

namespace VideoPlayer.ViewModels
{
    public class MediaPlayerUserControlViewModel : INotifyPropertyChanged
    {
        private readonly MediaPlayerUserControl userControl;
        private double loopStart;
        private double loopEnd;
        private WindowState oldState;
        private bool setStartLoop = true;
        private bool loopVideo;

        [DllImport("user32.dll")]
        private static extern uint GetDoubleClickTime();

        public readonly MainWindow MainWindow;
        public bool DarkTheme;
        public bool Seeking;
        public TimeSpan position;
        public double OldVolume;
        public bool LoopSpecificTime;
        public DispatcherTimer ProgressTimer;
        public bool IsFullscreen;
        public DispatcherTimer DoubleClickTimer;
        public DispatcherTimer ControlsTimer;
        public event PropertyChangedEventHandler PropertyChanged;
        public bool MouseOverControls;
        public static bool ChangingVolume;
        public static bool ChangingProgress;
        public bool IsPlaying;

        public bool LoopVideo
        {
            get => loopVideo;
            set
            {
                loopVideo = value;
                userControl.menuItemGridPlayerLoop.IsChecked = value;
                MainWindow.toggleSwitchLoop.IsOn = value;
            }
        }

        private void OnPropertyChanged(string prop)
        {
            if (!string.IsNullOrWhiteSpace(prop))
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        public MediaPlayerUserControlViewModel(MediaPlayerUserControl mediaPlayerUserControl, MainWindow mainWindow)
        {
            userControl = mediaPlayerUserControl;
            MainWindow = mainWindow;
            DoubleClickTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(GetDoubleClickTime())
            };
            DoubleClickTimer.Tick += (s, e) => DoubleClickTimer.Stop();

            ProgressTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(300)
            };
            ProgressTimer.Tick += ProgressTimer_Tick;

            ControlsTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1.5)
            };
            ControlsTimer.Tick += ControlsTimer_Tick;

            Thread thread = new Thread(() =>
            {
                while (MainWindow is null) { }
                Dispatcher.CurrentDispatcher.Invoke(() => SetPlayerVolume(Settings.CurrentSettings.Volume));
            });
            thread.Start();
        }

        private async void ProgressTimer_Tick(object sender, EventArgs e)
        {
            if (userControl.player.IsOpen && userControl.player.IsPlaying)
            {
                if (LoopSpecificTime && loopEnd > loopStart)
                {
                    if (userControl.sliderProgress.Value < loopStart ||
                        userControl.sliderProgress.Value >= loopEnd)
                    {
                        ProgressTimer.Stop();
                        await userControl.player.Pause();
                        Seeking = true;

                        int pos = Convert.ToInt32(loopStart);
                        await Seek(new TimeSpan(0, 0, 0, pos, 0));
                    }
                }

                if (!Seeking)
                    userControl.sliderProgress.Value = userControl.player.Position.TotalSeconds;
            }
        }

        private void ControlsTimer_Tick(object sender, EventArgs e)
        {
            if (userControl.player.IsPlaying &&
                !MainWindow.ShowTitleBar &&
                !MainWindow.ViewModel.IsAnyContextOpen &&
                !MainWindow.flyoutPlaylist.IsOpen &&
                !MainWindow.IsAnyDialogOpen &&
                !MouseOverControls)
            {
                userControl.gridControls.IsEnabled = false;
                userControl.gridControls.Visibility = Visibility.Hidden;
                Mouse.OverrideCursor = Cursors.None;

                MainWindow.flyoutCredits.IsOpen = false;
                MainWindow.flyoutPlaylist.IsOpen = false;
                MainWindow.flyoutPlaylists.IsOpen = false;
                MainWindow.flyoutQueue.IsOpen = false;
                MainWindow.flyoutSettings.IsOpen = false;
            }

            ControlsTimer.Stop();
        }

        public async Task Seek(TimeSpan timeSpan)
        {
            await userControl.player.Seek(timeSpan);
            MainWindow.ViewModel.SoundProcessor.Seek(timeSpan);
            Seeking = false;
            if (IsPlaying)
            {
                ProgressTimer.Start();
                await userControl.player.Play();
                MainWindow.ViewModel.SoundProcessor.Play();
            }
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

        public async Task OpenDialog()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Select video file(s)",
                DefaultExt = ".avi",
                Filter = "Media Files|*.mpg;*.avi;*.wma;*.mov;*.wav;*.mp2;*.mp3;*.mp4|All Files|*.*",
                Multiselect = true
            };

            if (openFileDialog.ShowDialog() == true)
            {
                List<string> fileNames = openFileDialog.FileNames.ToList();
                List<Media> medias = new List<Media>();
                fileNames.ForEach(x => medias.Add(new Media(x)));

                await MainWindow.ViewModel.AddMediasToQueue(medias);
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
            MainWindow.ViewModel.SoundProcessor.OpenFile(media);
            await Play();
            ChangeSpeed(MainWindow.numericPlaybackSpeed.Value.GetValueOrDefault());
            ChangePitch((int)MainWindow.numericPitch.Value.GetValueOrDefault());
        }

        public async Task Play()
        {
            IsPlaying = true;
            ProgressTimer.Start();
            await userControl.player.Play();
            MainWindow.ViewModel.SoundProcessor.Play();
            userControl.iconPlayPause.Kind = PackIconKind.Pause;
            EnablePlayPause();
            EnableStop();
        }

        public async Task Pause()
        {
            IsPlaying = false;
            ProgressTimer.Stop();
            await userControl.player.Pause();
            MainWindow.ViewModel.SoundProcessor.Pause();
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
                ResetControls();
            }
            else
                await userControl.player.Stop();

            MainWindow.ViewModel.SoundProcessor.Stop();
        }

        public void ResetControls()
        {
            MainWindow.textBoxLoopStart.IsEnabled = false;
            MainWindow.textBoxLoopEnd.IsEnabled = false;
            ResetLoop();
            MainWindow.toggleSwitchLoopTime.IsEnabled = false;
            MainWindow.toggleSwitchLoopTime.IsOn = false;
            userControl.sliderProgress.IsEnabled = false;
        }

        public void ResetProgress()
        {
            userControl.sliderProgress.Value = 0;
            userControl.textBlockDuration.Text = "0:00 / 0:00";
        }

        public async Task MuteToggle()
        {
            if (MainWindow.ViewModel.SoundProcessor.Volume > 0)
            {
                OldVolume = MainWindow.ViewModel.SoundProcessor.Volume;
                Settings.CurrentSettings.Volume = 0;
            }
            else if (OldVolume == 0)
            {
                Settings.CurrentSettings.Volume = 1;
                OldVolume = MainWindow.ViewModel.SoundProcessor.Volume;
            }
            else
            {
                Settings.CurrentSettings.Volume = OldVolume;
                OldVolume = MainWindow.ViewModel.SoundProcessor.Volume;
            }

            await Settings.CurrentSettings.Save();
        }

        public void ChangeSpeed(double speed)
        {
            userControl.player.SpeedRatio = speed;
            int newSpeed;

            if (speed < 1)
                newSpeed = (int)(0 - ((1 - speed) * 100));
            else
                newSpeed = (int)(0 + (((1 - speed) * 100) * -1));

            MainWindow.ViewModel.SoundProcessor.Tempo = newSpeed;
        }

        public void ChangePitch(int pitch)
        {
            MainWindow.ViewModel.SoundProcessor.Pitch = pitch;
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
                    timeSpanString = @"hh\:";
                else
                    timeSpanString = @"h\:";
            }
            if (timeSpan.Minutes > 9 || timeSpan.Hours > 0)
                timeSpanString += @"mm\:";
            else
                timeSpanString += @"m\:";

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

                if (loopStart > 0 && loopEnd > 0 && loopStart < loopEnd)
                {
                    SetSelection(loopStart, loopEnd);
                    userControl.buttonResetLoop.IsEnabled = true;
                }
                else
                    userControl.buttonResetLoop.IsEnabled = false;
            }
            else
            {
                loopEnd = 0;
                loopStart = 0;
                userControl.buttonResetLoop.IsEnabled = false;
            }
        }

        private string Split(string source, int start, int end)
        {
            if (end < 0)
                end = source.Length + end;
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

            if (loopStart == 0 && setStartLoop)
            {
                loopStart = value;
                MainWindow.textBoxLoopStart.Text = ConvertSecondsToTime(value);
                setStartLoop = false;
            }
            else if (loopEnd == 0 && !setStartLoop)
            {
                loopEnd = value;
                MainWindow.textBoxLoopEnd.Text = ConvertSecondsToTime(value);
                MainWindow.toggleSwitchLoopTime.IsOn = true;
                setStartLoop = true;
            }
            else if (setStartLoop)
            {
                loopStart = value;
                MainWindow.textBoxLoopStart.Text = ConvertSecondsToTime(value);
                setStartLoop = false;
            }
            else
            {
                loopEnd = value;
                MainWindow.textBoxLoopEnd.Text = ConvertSecondsToTime(value);
                setStartLoop = true;
            }
        }

        public void SetSelection(double start, double end)
        {
            userControl.sliderProgress.SelectionStart = start;
            userControl.sliderProgress.SelectionEnd = end;
        }

        public void ResetLoop()
        {
            MainWindow.textBoxLoopStart.Text = "0:00";
            MainWindow.textBoxLoopEnd.Text = "0:00";
            MainWindow.toggleSwitchLoopTime.IsOn = false;
        }

        public async Task PreviousTrack()
        {
            MainWindow.ViewModel.Queue.Insert(0, MainWindow.ViewModel.SelectedMedia);
            MainWindow.ViewModel.SelectedMedia = MainWindow.ViewModel.OldQueue[MainWindow.ViewModel.OldQueue.Count - 1];
            MainWindow.ViewModel.OldQueue.Remove(MainWindow.ViewModel.SelectedMedia);
            await Open(MainWindow.ViewModel.SelectedMedia);

            if (MainWindow.ViewModel.OldQueue.Count == 0)
                userControl.buttonSkipBackwards.IsEnabled = false;
            else
                userControl.buttonSkipBackwards.IsEnabled = true;

            userControl.buttonSkipForward.IsEnabled = true;
        }

        public async Task ChangeTrack(int index)
        {
            Media media = MainWindow.ViewModel.Queue[index];
            await Open(media);

            if (index + 1 == MainWindow.ViewModel.Queue.Count)
                userControl.buttonSkipForward.IsEnabled = false;
            else
                userControl.buttonSkipForward.IsEnabled = true;

            List<Media> oldMedias = MainWindow.ViewModel.Queue.Where(x => MainWindow.ViewModel.Queue.IndexOf(x) < index).ToList();
            List<Media> medias = MainWindow.ViewModel.Queue.Where(x => MainWindow.ViewModel.Queue.IndexOf(x) > index).ToList();
            MainWindow.ViewModel.Queue = new ObservableCollection<Media>(medias);

            if (MainWindow.ViewModel.SelectedMedia != null)
                MainWindow.ViewModel.OldQueue.Add(MainWindow.ViewModel.SelectedMedia);

            foreach (Media oldMedia in oldMedias)
                MainWindow.ViewModel.OldQueue.Add(oldMedia);

            MainWindow.ViewModel.SelectedMedia = media;

            if (MainWindow.ViewModel.OldQueue.Count > 0)
                userControl.buttonSkipBackwards.IsEnabled = true;
            else
                userControl.buttonSkipBackwards.IsEnabled = false;
        }

        public async Task ChangeTrack(Media media)
        {
            int index = MainWindow.ViewModel.Queue.IndexOf(media);
            await Open(media);

            if (index + 1 == MainWindow.ViewModel.Queue.Count)
                userControl.buttonSkipForward.IsEnabled = false;
            else
                userControl.buttonSkipForward.IsEnabled = true;

            List<Media> oldMedias = MainWindow.ViewModel.Queue.Where(x => MainWindow.ViewModel.Queue.IndexOf(x) < index).ToList();
            List<Media> medias = MainWindow.ViewModel.Queue.Where(x => MainWindow.ViewModel.Queue.IndexOf(x) > index).ToList();
            MainWindow.ViewModel.Queue = new ObservableCollection<Media>(medias);

            if (MainWindow.ViewModel.SelectedMedia != null)
                MainWindow.ViewModel.OldQueue.Add(MainWindow.ViewModel.SelectedMedia);

            foreach (Media oldMedia in oldMedias)
                MainWindow.ViewModel.OldQueue.Add(oldMedia);

            MainWindow.ViewModel.SelectedMedia = media;

            if (MainWindow.ViewModel.OldQueue.Count > 0)
                userControl.buttonSkipBackwards.IsEnabled = true;
            else
                userControl.buttonSkipBackwards.IsEnabled = false;
        }

        public Playlist GetPlaylist(string name, string[] filePaths)
        {
            List<Media> medias = new List<Media>();
            filePaths.ToList().ForEach(x => medias.Add(new Media(x)));
            Playlist playlist = new Playlist(name, medias);

            return playlist;
        }

        public async Task SkipForward(int value)
        {
            if (IsFullscreen)
                ShowControlsInFullscreen();

            int pos = Convert.ToInt32(userControl.sliderProgress.Value + value);
            await userControl.player.Seek(new TimeSpan(0, 0, 0, pos, 0));
            userControl.sliderProgress.Value = userControl.player.Position.TotalSeconds;
        }

        public async Task SkipBackwards(int value)
        {
            if (IsFullscreen)
                ShowControlsInFullscreen();

            int pos = Convert.ToInt32(userControl.sliderProgress.Value - value);
            await userControl.player.Seek(new TimeSpan(0, 0, 0, pos, 0));
            userControl.sliderProgress.Value = userControl.player.Position.TotalSeconds;
        }

        public async Task PlayPause()
        {
            if (userControl.player.IsOpen)
            {
                if (IsFullscreen)
                    ShowControlsInFullscreen();

                if (userControl.player.IsPlaying)
                    await Pause();
                else
                    await Play();
            }
        }

        public void EnterFullscreen()
        {
            MainWindow.ShowTitleBar = false;
            MainWindow.ShowCloseButton = false;
            MainWindow.ShowMaxRestoreButton = false;
            MainWindow.ShowMinButton = false;
            MainWindow.IgnoreTaskbarOnMaximize = true;
            oldState = MainWindow.WindowState;
            MainWindow.WindowStyle = WindowStyle.None;
            if (oldState != WindowState.Maximized)
                MainWindow.WindowState = WindowState.Maximized;
            MainWindow.Topmost = true;
            userControl.gridMediaElementBackground.SetValue(Grid.RowSpanProperty, 3);
            userControl.gridControls.Opacity = 0.7;
            MainWindow.flyoutQueue.Opacity = 0.7;
            MainWindow.flyoutCredits.Opacity = 0.7;
            MainWindow.flyoutPlaylists.Opacity = 0.7;
            MainWindow.flyoutSettings.Opacity = 0.7;
            IsFullscreen = true;
            ControlsTimer.Start();
            userControl.iconFullscreen.Kind = PackIconKind.FullscreenExit;
        }

        public void ExitFullscreen()
        {
            IsFullscreen = false;
            ControlsTimer.Stop();
            MainWindow.IgnoreTaskbarOnMaximize = false;
            MainWindow.Topmost = Settings.CurrentSettings.AlwaysOnTop;
            MainWindow.WindowState = oldState;
            MainWindow.WindowStyle = WindowStyle.SingleBorderWindow;
            MainWindow.ShowTitleBar = true;
            MainWindow.ShowCloseButton = true;
            MainWindow.ShowMaxRestoreButton = true;
            MainWindow.ShowMinButton = true;
            userControl.gridMediaElementBackground.ClearValue(Grid.RowSpanProperty);
            userControl.gridControls.IsEnabled = true;
            userControl.gridControls.Visibility = Visibility.Visible;
            userControl.gridControls.Opacity = 1;
            MainWindow.flyoutQueue.Opacity = 1;
            MainWindow.flyoutCredits.Opacity = 1;
            MainWindow.flyoutPlaylists.Opacity = 1;
            MainWindow.flyoutSettings.Opacity = 1;
            Mouse.OverrideCursor = null;
            userControl.iconFullscreen.Kind = PackIconKind.Fullscreen;
        }

        public void ShowControlsInFullscreen()
        {
            userControl.gridControls.IsEnabled = true;
            userControl.gridControls.Visibility = Visibility.Visible;
            ControlsTimer.Stop();
            ControlsTimer.Start();
            Mouse.OverrideCursor = Cursors.Arrow;
        }

        public void SetPlayerVolume(double volume)
        {
            //userControl.player.Volume = volume;
            MainWindow.ViewModel.SoundProcessor.Volume = volume;

            userControl.Dispatcher.Invoke(() =>
            {
                if (volume == 0)
                    userControl.iconVolume.Kind = PackIconKind.VolumeMute;
                else if (volume <= .3)
                    userControl.iconVolume.Kind = PackIconKind.VolumeLow;
                else if (volume <= .6)
                    userControl.iconVolume.Kind = PackIconKind.VolumeMedium;
                else
                    userControl.iconVolume.Kind = PackIconKind.VolumeHigh;
            });
        }
    }
}
