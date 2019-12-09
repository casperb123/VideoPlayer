using Microsoft.Win32;
using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using VideoPlayer.Entities;

namespace VideoPlayer.UserControls.ViewModels
{
    public class MediaPlayerUserControlViewModel
    {
        private readonly MediaPlayerUserControl mediaPlayerUserControl;

        public readonly Image PlayImage;
        public readonly Image PlayImageDisabled;

        public readonly Image PauseImage;
        public readonly Image PauseImageDisabled;

        public readonly Image StopImage;
        public readonly Image StopImageDisabled;

        public readonly Image OpenImage;
        public readonly Image OpenImageDisabled;

        public readonly Image MutedImage;
        public readonly Image MutedImageDisabled;

        public readonly Image HighVolume;
        public readonly Image HighVolumeDisabled;

        public readonly Image LowVolume;
        public readonly Image LowVolumeDisabled;

        public TimeSpan position;
        public double OldVolume;

        public DispatcherTimer ProgressTimer;

        public MediaPlayerUserControlViewModel(MediaPlayerUserControl mediaPlayerUserControl, string filePath)
            : this(mediaPlayerUserControl)
        {
            Open(filePath);
        }

        public MediaPlayerUserControlViewModel(MediaPlayerUserControl mediaPlayerUserControl)
        {
            this.mediaPlayerUserControl = mediaPlayerUserControl;

            mediaPlayerUserControl.Focusable = true;
            mediaPlayerUserControl.Loaded += (s, e) => Keyboard.Focus(mediaPlayerUserControl);

            mediaPlayerUserControl.mediaElementBackground.Background = new SolidColorBrush(Color.FromRgb(16, 16, 16));

            string runningPath = AppDomain.CurrentDomain.BaseDirectory;
            string resourcesPath = $@"{Path.GetFullPath(Path.Combine(runningPath, @"..\..\..\"))}Resources";

            if (!Directory.Exists(resourcesPath))
            {
                resourcesPath = $@"{runningPath}\Resources";
            }

            PlayImage = new Image
            {
                Width = mediaPlayerUserControl.buttonPlayPause.Width,
                Source = new BitmapImage(new Uri($@"{resourcesPath}\play.png"))
            };
            PlayImageDisabled = new Image
            {
                Width = mediaPlayerUserControl.buttonPlayPause.Width,
                Source = new BitmapImage(new Uri($@"{resourcesPath}\play-disabled.png"))
            };

            PauseImage = new Image
            {
                Width = mediaPlayerUserControl.buttonPlayPause.Width,
                Source = new BitmapImage(new Uri($@"{resourcesPath}\pause.png"))
            };
            PauseImageDisabled = new Image
            {
                Width = mediaPlayerUserControl.buttonPlayPause.Width,
                Source = new BitmapImage(new Uri($@"{resourcesPath}\pause-disabled.png"))
            };

            StopImage = new Image
            {
                Width = mediaPlayerUserControl.buttonStop.Width,
                Source = new BitmapImage(new Uri($@"{resourcesPath}\stop.png"))
            };
            StopImageDisabled = new Image
            {
                Width = mediaPlayerUserControl.buttonStop.Width,
                Source = new BitmapImage(new Uri($@"{resourcesPath}\stop-disabled.png"))
            };

            OpenImage = new Image
            {
                Width = mediaPlayerUserControl.buttonOpen.Width,
                Source = new BitmapImage(new Uri($@"{resourcesPath}\open.png"))
            };
            OpenImageDisabled = new Image
            {
                Width = mediaPlayerUserControl.buttonOpen.Width,
                Source = new BitmapImage(new Uri($@"{resourcesPath}\open-disabled.png"))
            };

            MutedImage = new Image
            {
                Width = mediaPlayerUserControl.buttonMuteUnmute.Width,
                Source = new BitmapImage(new Uri($@"{resourcesPath}\muted.png"))
            };
            MutedImageDisabled = new Image
            {
                Width = mediaPlayerUserControl.buttonMuteUnmute.Width,
                Source = new BitmapImage(new Uri($@"{resourcesPath}\muted-disabled.png"))
            };

            HighVolume = new Image
            {
                Width = mediaPlayerUserControl.buttonMuteUnmute.Width,
                Source = new BitmapImage(new Uri($@"{resourcesPath}\high-volume.png"))
            };
            HighVolumeDisabled = new Image
            {
                Width = mediaPlayerUserControl.buttonMuteUnmute.Width,
                Source = new BitmapImage(new Uri($@"{resourcesPath}\high-volume-disabled.png"))
            };

            LowVolume = new Image
            {
                Width = mediaPlayerUserControl.buttonMuteUnmute.Width,
                Source = new BitmapImage(new Uri($@"{resourcesPath}\low-volume.png"))
            };
            LowVolumeDisabled = new Image
            {
                Width = mediaPlayerUserControl.buttonMuteUnmute.Width,
                Source = new BitmapImage(new Uri($@"{resourcesPath}\low-volume-disabled.png"))
            };

            mediaPlayerUserControl.imagePlayPause.Source = PlayImageDisabled.Source;
            mediaPlayerUserControl.imageStop.Source = StopImageDisabled.Source;
            mediaPlayerUserControl.imageOpen.Source = OpenImage.Source;
            mediaPlayerUserControl.imageMuteUnmute.Source = HighVolume.Source;

            ProgressTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(1000)
            };
            ProgressTimer.Tick += ProgressTimer_Tick;
        }
        private void ProgressTimer_Tick(object sender, EventArgs e)
        {
            mediaPlayerUserControl.sliderProgress.Value = mediaPlayerUserControl.player.Position.TotalSeconds;
        }

        public bool IsPlaying
        {
            get
            {
                switch (GetMediaState(mediaPlayerUserControl.player))
                {
                    case MediaState.Manual:
                        return false;
                    case MediaState.Play:
                        return true;
                    case MediaState.Close:
                        return false;
                    case MediaState.Pause:
                        return false;
                    case MediaState.Stop:
                        return false;
                    default:
                        return false;
                }
            }
            set
            {
                if (value)
                {
                    Play();
                }
                else
                {
                    Pause();
                }
            }
        }

        public bool IsPaused
        {
            get
            {
                switch (GetMediaState(mediaPlayerUserControl.player))
                {
                    case MediaState.Manual:
                        return false;
                    case MediaState.Play:
                        return false;
                    case MediaState.Close:
                        return false;
                    case MediaState.Pause:
                        return true;
                    case MediaState.Stop:
                        return false;
                    default:
                        return false;
                }
            }
            set
            {
                if (value)
                {
                    Pause();
                }
                else
                {
                    Play();
                }
            }
        }

        private MediaState GetMediaState(MediaElement mediaElement)
        {
            FieldInfo hlp = typeof(MediaElement).GetField("_helper", BindingFlags.NonPublic | BindingFlags.Instance);
            object helperObject = hlp.GetValue(mediaElement);
            FieldInfo stateField = helperObject.GetType().GetField("_currentState", BindingFlags.NonPublic | BindingFlags.Instance);
            MediaState state = (MediaState)stateField.GetValue(helperObject);
            return state;
        }

        public void ShowTime()
        {
            if (mediaPlayerUserControl.player.NaturalDuration.HasTimeSpan)
            {
                TimeSpan currentTime = TimeSpan.FromSeconds(mediaPlayerUserControl.sliderProgress.Value);
                mediaPlayerUserControl.textBlockDuration.Text = $"{currentTime.ToString(@"m\:ss")} / {mediaPlayerUserControl.player.NaturalDuration.TimeSpan.ToString(@"m\:ss")}";
            }
        }

        public void DisablePlayPause()
        {
            mediaPlayerUserControl.buttonPlayPause.IsEnabled = false;

            if (mediaPlayerUserControl.imagePlayPause.Source == PlayImage.Source)
            {
                mediaPlayerUserControl.imagePlayPause.Source = PlayImageDisabled.Source;
            }
            else if (mediaPlayerUserControl.imagePlayPause.Source == PauseImage.Source)
            {
                mediaPlayerUserControl.imagePlayPause.Source = PauseImageDisabled.Source;
            }
        }

        public void EnablePlayPause()
        {
            mediaPlayerUserControl.buttonPlayPause.IsEnabled = true;

            if (mediaPlayerUserControl.imagePlayPause.Source == PlayImageDisabled.Source)
            {
                mediaPlayerUserControl.imagePlayPause.Source = PlayImage.Source;
            }
            else if (mediaPlayerUserControl.imagePlayPause.Source == PauseImageDisabled.Source)
            {
                mediaPlayerUserControl.imagePlayPause.Source = PauseImage.Source;
            }
        }

        public void DisableStop()
        {
            mediaPlayerUserControl.buttonStop.IsEnabled = false;
            mediaPlayerUserControl.imageStop.Source = StopImageDisabled.Source;
        }

        public void EnableStop()
        {
            mediaPlayerUserControl.buttonStop.IsEnabled = true;
            mediaPlayerUserControl.imageStop.Source = StopImage.Source;
        }

        public void Open(string filePath)
        {
            if (mediaPlayerUserControl.player.NaturalDuration.HasTimeSpan)
            {
                Stop();
            }

            Media video = new Media(filePath);
            mediaPlayerUserControl.player.Source = video.Uri;
            Play();
        }

        public void Play()
        {
            ProgressTimer.Start();
            mediaPlayerUserControl.player.Play();
            mediaPlayerUserControl.imagePlayPause.Source = PauseImage.Source;
        }

        public void Pause()
        {
            ProgressTimer.Stop();
            mediaPlayerUserControl.player.Pause();
            mediaPlayerUserControl.imagePlayPause.Source = PlayImage.Source;
        }

        public void Stop()
        {
            mediaPlayerUserControl.player.Close();
            mediaPlayerUserControl.player.Source = null;

            DisablePlayPause();
            DisableStop();

            ProgressTimer.Stop();
            mediaPlayerUserControl.sliderProgress.Value = 0;
            mediaPlayerUserControl.textBlockDuration.Text = "0:00 / 0:00";
        }

        public bool MuteToggle()
        {
            if (mediaPlayerUserControl.player.Volume > 0)
            {
                OldVolume = mediaPlayerUserControl.player.Volume;
                mediaPlayerUserControl.sliderVolume.Value = 0;

                return true;
            }
            else if (OldVolume == 0)
            {
                mediaPlayerUserControl.sliderVolume.Value = 1;
                OldVolume = mediaPlayerUserControl.player.Volume;
            }
            else
            {
                mediaPlayerUserControl.sliderVolume.Value = OldVolume;
                OldVolume = mediaPlayerUserControl.player.Volume;
            }

            return false;
        }

        public void ButtonPlayPause_Click(object sender, RoutedEventArgs e)
        {
            MediaPlayerUserControl.PlayPauseCmd.Execute(null, mediaPlayerUserControl.buttonPlayPause);
            mediaPlayerUserControl.Focus();
        }

        private void ButtonStop_Click(object sender, RoutedEventArgs e)
        {
            Stop();
            mediaPlayerUserControl.Focus();
        }

        private void ButtonOpen_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Select video file",
                DefaultExt = ".avi",
                Filter = "Media Files|*.mpg;*.avi;*.wma;*.mov;*.wav;*.mp2;*.mp3;*.mp4|All Files|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                Open(openFileDialog.FileName);
            }

            mediaPlayerUserControl.Focus();
        }

        private void Player_MediaEnded(object sender, RoutedEventArgs e)
        {
            Stop();
        }

        private void Player_MediaOpened(object sender, RoutedEventArgs e)
        {
            Play();

            position = mediaPlayerUserControl.player.NaturalDuration.TimeSpan;
            mediaPlayerUserControl.sliderProgress.Minimum = 0;
            mediaPlayerUserControl.sliderProgress.Maximum = position.TotalSeconds;

            EnablePlayPause();
            EnableStop();

            ProgressTimer.Start();
        }

        private void SliderProgress_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            mediaPlayerUserControl.player.Pause();
            ProgressTimer.Stop();
        }

        private void SliderProgress_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            int pos = Convert.ToInt32(mediaPlayerUserControl.sliderProgress.Value);
            mediaPlayerUserControl.player.Position = new TimeSpan(0, 0, 0, pos, 0);
            mediaPlayerUserControl.player.Play();
            ProgressTimer.Start();
        }

        private void SliderProgress_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ShowTime();
        }

        private void SliderVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!mediaPlayerUserControl.IsLoaded) return;

            mediaPlayerUserControl.player.Volume = mediaPlayerUserControl.sliderVolume.Value;

            if (mediaPlayerUserControl.sliderVolume.Value == 0)
            {
                mediaPlayerUserControl.imageMuteUnmute.Source = MutedImage.Source;
            }
            else if (mediaPlayerUserControl.sliderVolume.Value <= .5)
            {
                mediaPlayerUserControl.imageMuteUnmute.Source = LowVolume.Source;
            }
            else
            {
                mediaPlayerUserControl.imageMuteUnmute.Source = HighVolume.Source;
            }
        }

        private void SliderVolume_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            OldVolume = mediaPlayerUserControl.sliderVolume.Value;
        }

        private void SliderVolume_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (mediaPlayerUserControl.sliderVolume.Value > 0)
            {
                OldVolume = mediaPlayerUserControl.sliderVolume.Value;
            }
        }

        private void ButtonMuteUnmute_Click(object sender, RoutedEventArgs e)
        {
            MuteToggle();
            mediaPlayerUserControl.Focus();
        }

        private void UserControl_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                IsPlaying = !IsPlaying;
            }
            else if (e.Key == Key.Right)
            {
                int pos = Convert.ToInt32(mediaPlayerUserControl.sliderProgress.Value + 5);
                mediaPlayerUserControl.player.Position = new TimeSpan(0, 0, 0, pos, 0);
                mediaPlayerUserControl.sliderProgress.Value = mediaPlayerUserControl.player.Position.TotalSeconds;
            }
            else if (e.Key == Key.Left)
            {
                int pos = Convert.ToInt32(mediaPlayerUserControl.sliderProgress.Value - 5);
                mediaPlayerUserControl.player.Position = new TimeSpan(0, 0, 0, pos, 0);
                mediaPlayerUserControl.sliderProgress.Value = mediaPlayerUserControl.player.Position.TotalSeconds;
            }
        }
    }
}
