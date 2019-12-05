using Microsoft.Win32;
using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using VideoPlayer.Entities;

namespace VideoPlayer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private TimeSpan position;
        private DispatcherTimer progressTimer;

        private bool holdingLeftMouse;

        private void Play()
        {
            player.Play();
            buttonPlayPause.Content = "Pause";
        }

        private void Pause()
        {
            player.Pause();
            buttonPlayPause.Content = "Play";
        }

        public MainWindow()
        {
            InitializeComponent();
            Unosquare.FFME.Library.FFmpegDirectory = @$"{Directory.GetCurrentDirectory()}\ffmpeg";

            progressTimer = new DispatcherTimer();
            progressTimer.Interval = TimeSpan.FromMilliseconds(1000);
            progressTimer.Tick += ProgressTimer_Tick;
            progressTimer.Start();
        }

        private void ProgressTimer_Tick(object sender, EventArgs e)
        {
            sliderProgress.Value = player.Position.TotalSeconds;
        }

        private void buttonOpen_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Select video file",
                InitialDirectory = Directory.GetCurrentDirectory(),
                DefaultExt = ".avi",
                Filter = "Media Files|*.mpg;*.avi;*.wma;*.mov;*.wav;*.mp2;*.mp3;*.mp4|All Files|*.*",
            };
            openFileDialog.ShowDialog();

            Video video = new Video(openFileDialog.FileName);

            player.Source = video.Uri;
            Play();
        }

        private void buttonPlayPause_Click(object sender, RoutedEventArgs e)
        {
            if (player.Source != null)
            {
                if (player.IsPlaying)
                {
                    Pause();
                }
                else
                {
                    Play();
                }
            }
        }

        private void player_MediaEnded(object sender, EventArgs e)
        {
            Pause();
            buttonPlayPause.IsEnabled = false;
            buttonOpen.IsEnabled = true;
        }

        private void player_MediaOpened(object sender, Unosquare.FFME.Common.MediaOpenedEventArgs e)
        {
            position = player.NaturalDuration.Value;
            sliderProgress.Minimum = 0;
            sliderProgress.Maximum = position.TotalSeconds;

            buttonPlayPause.IsEnabled = true;
            buttonOpen.IsEnabled = false;
        }
        private void sliderProgress_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            holdingLeftMouse = true;
            player.Pause();
            progressTimer.Stop();
        }

        private void sliderProgress_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            holdingLeftMouse = false;
            player.Play();
            progressTimer.Start();
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                if (player.IsPlaying)
                {
                    Pause();
                }
                else
                {
                    Play();
                }
            }
            else if (e.Key == Key.Right)
            {
                int pos = Convert.ToInt32(sliderProgress.Value + 5);
                player.Position = new TimeSpan(0, 0, 0, pos, 0);
                sliderProgress.Value = player.Position.TotalSeconds;
            }
            else if (e.Key == Key.Left)
            {
                int pos = Convert.ToInt32(sliderProgress.Value - 5);
                player.Position = new TimeSpan(0, 0, 0, pos, 0);
                sliderProgress.Value = player.Position.TotalSeconds;
            }
        }

        private void sliderProgress_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (holdingLeftMouse)
            {
                int pos = Convert.ToInt32(sliderProgress.Value);
                player.Position = new TimeSpan(0, 0, 0, pos, 0);
                sliderProgress.Value = player.Position.TotalSeconds;
            }
        }
    }
}
