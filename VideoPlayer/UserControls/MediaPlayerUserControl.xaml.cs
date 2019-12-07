using Microsoft.Win32;
using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using VideoPlayer.Entities;

namespace VideoPlayer.UserControls
{
    /// <summary>
    /// Interaction logic for MediaPlayer.xaml
    /// </summary>
    public partial class MediaPlayerUserControl : UserControl
    {
        private readonly Image playImage;
        private readonly Image pauseImage;
        private readonly Image stopImage;
        private readonly Image openImage;
        private readonly Image mutedImage;
        private readonly Image unmutedImage;

        private TimeSpan position;
        private double oldVolume;

        private DispatcherTimer progressTimer;

        public bool IsPlaying
        {
            get
            {
                switch (GetMediaState(player))
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
                switch (GetMediaState(player))
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

        private void ShowTime()
        {
            if (player.NaturalDuration.HasTimeSpan)
            {
                TimeSpan currentTime = TimeSpan.FromSeconds(sliderProgress.Value);
                textBlockDuration.Text = $"{currentTime.ToString(@"m\:ss")} / {player.NaturalDuration.TimeSpan.ToString(@"m\:ss")}";
            }
        }

        private void Play()
        {
            progressTimer.Start();
            player.Play();
            imagePlayPause.Source = pauseImage.Source;
        }

        private void Pause()
        {
            progressTimer.Stop();
            player.Pause();
            imagePlayPause.Source = playImage.Source;
        }

        private void Stop()
        {
            player.Close();

            buttonPlayPause.IsEnabled = false;
            //buttonPlayPause.Content = "Play";
            buttonStop.IsEnabled = false;

            progressTimer.Stop();
            sliderProgress.Value = 0;
            textBlockDuration.Text = "0:00 / 0:00";
        }

        private void MuteToggle()
        {
            if (player.Volume > 0)
            {
                oldVolume = player.Volume;
                sliderVolume.Value = 0;
                imageMuteUnmute.Source = mutedImage.Source;
            }
            else if (oldVolume == 0)
            {
                sliderVolume.Value = 1;
                oldVolume = player.Volume;
                imageMuteUnmute.Source = unmutedImage.Source;
            }
            else
            {
                sliderVolume.Value = oldVolume;
                oldVolume = player.Volume;
                imageMuteUnmute.Source = unmutedImage.Source;
            }
        }

        public MediaPlayerUserControl()
        {
            InitializeComponent();
            mediaElementBackground.Background = new SolidColorBrush(Color.FromRgb(16, 16, 16));

            string runningPath = AppDomain.CurrentDomain.BaseDirectory;
            string resourcesPath = $@"{Path.GetFullPath(Path.Combine(runningPath, @"..\..\..\"))}Resources";

            if (!Directory.Exists(resourcesPath))
            {
                resourcesPath = $@"{runningPath}\Resources";
            }

            playImage = new Image
            {
                Width = buttonPlayPause.Width,
                Source = new BitmapImage(new Uri($@"{resourcesPath}\play.png"))
            };

            pauseImage = new Image
            {
                Width = buttonPlayPause.Width,
                Source = new BitmapImage(new Uri($@"{resourcesPath}\pause.png"))
            };

            stopImage = new Image
            {
                Width = buttonStop.Width,
                Source = new BitmapImage(new Uri($@"{resourcesPath}\stop.png"))
            };

            openImage = new Image
            {
                Width = buttonOpen.Width,
                Source = new BitmapImage(new Uri($@"{resourcesPath}\open.png"))
            };

            mutedImage = new Image
            {
                Width = buttonMuteUnmute.Width,
                Source = new BitmapImage(new Uri($@"{resourcesPath}\muted.png"))
            };

            unmutedImage = new Image
            {
                Width = buttonMuteUnmute.Width,
                Source = new BitmapImage(new Uri($@"{resourcesPath}\unmuted.png"))
            };

            imagePlayPause.Source = playImage.Source;
            imageStop.Source = stopImage.Source;
            imageOpen.Source = openImage.Source;
            imageMuteUnmute.Source = unmutedImage.Source;

            progressTimer = new DispatcherTimer();
            progressTimer.Interval = TimeSpan.FromMilliseconds(1000);
            progressTimer.Tick += ProgressTimer_Tick;
        }

        private void ProgressTimer_Tick(object sender, EventArgs e)
        {
            sliderProgress.Value = player.Position.TotalSeconds;
        }

        private void ButtonPlayPause_Click(object sender, RoutedEventArgs e)
        {
            if (player.Source != null)
            {
                IsPlaying = !IsPlaying;
            }
        }

        private void ButtonStop_Click(object sender, RoutedEventArgs e)
        {
            Stop();
        }

        private void ButtonOpen_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Select video file",
                DefaultExt = ".avi",
                Filter = "Media Files|*.mpg;*.avi;*.wma;*.mov;*.wav;*.mp2;*.mp3;*.mp4|All Files|*.*"
            };
            openFileDialog.ShowDialog();

            Media video = new Media(openFileDialog.FileName);
            player.Source = video.Uri;
            Play();
        }

        private void Player_MediaEnded(object sender, RoutedEventArgs e)
        {
            Stop();
        }

        private void Player_MediaOpened(object sender, RoutedEventArgs e)
        {
            Play();

            position = player.NaturalDuration.TimeSpan;
            sliderProgress.Minimum = 0;
            sliderProgress.Maximum = position.TotalSeconds;

            buttonPlayPause.IsEnabled = true;
            buttonStop.IsEnabled = true;

            progressTimer.Start();
        }

        private void SliderProgress_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            player.Pause();
            progressTimer.Stop();
        }

        private void SliderProgress_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            int pos = Convert.ToInt32(sliderProgress.Value);
            player.Position = new TimeSpan(0, 0, 0, pos, 0);
            player.Play();
            progressTimer.Start();
        }

        private void SliderProgress_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ShowTime();
        }

        private void SliderVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsLoaded) return;

            player.Volume = sliderVolume.Value;

            if (sliderVolume.Value == 0)
            {
                imageMuteUnmute.Source = mutedImage.Source;
            }
            else
            {
                imageMuteUnmute.Source = unmutedImage.Source;
            }
        }

        private void SliderVolume_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            oldVolume = sliderVolume.Value;
        }

        private void SliderVolume_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sliderVolume.Value > 0)
            {
                oldVolume = sliderVolume.Value;
            }
        }

        private void ButtonMuteUnmute_Click(object sender, RoutedEventArgs e)
        {
            MuteToggle();
        }
    }
}
