using Microsoft.Win32;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Unosquare.FFME.Common;
using VideoPlayer.ViewModels;
using MaterialDesignThemes.Wpf;
using VideoPlayer.Entities;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using MahApps.Metro.Controls;

namespace VideoPlayer.UserControls
{
    /// <summary>
    /// Interaction logic for MediaPlayer.xaml
    /// </summary>F
    public partial class MediaPlayerUserControl : UserControl
    {
        public readonly MediaPlayerUserControlViewModel ViewModel;

        public MediaPlayerUserControl(MainWindow mainWindow)
        {
            InitializeComponent();

            sliderProgress.AddHandler(PreviewMouseLeftButtonDownEvent, new MouseButtonEventHandler(SliderProgress_PreviewMouseLeftButtonDown), true);
            sliderProgress.AddHandler(PreviewMouseLeftButtonUpEvent, new MouseButtonEventHandler(SliderProgress_PreviewMouseLeftButtonUp), true);

            ViewModel = new MediaPlayerUserControlViewModel(this, mainWindow);
            DataContext = ViewModel;
        }

        public async void ButtonPlayPause_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.PlayPause();
            Focus();
        }

        private async void ButtonStop_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.Stop(false);
            Focus();
        }

        private async void ButtonAddToQueue_Click(object sender, RoutedEventArgs e)
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

                await ViewModel.MainWindow.ViewModel.AddMediasToQueue(medias);
            }

            Focus();
        }

        private async void Player_MediaEnded(object sender, EventArgs e)
        {
            if (ViewModel.LoopVideo)
            {
                await ViewModel.Stop(false);
                await ViewModel.Play();
            }
            else
            {
                if (ViewModel.MainWindow.ViewModel.Queue.Count > 0)
                {
                    ViewModel.MainWindow.dataGridQueue.SelectedIndex++;
                }
                else
                {
                    await ViewModel.Stop(false);
                }
            }
        }

        private async void Player_MediaOpened(object sender, MediaOpenedEventArgs e)
        {
            ViewModel.MainWindow.textBoxLoopStart.IsEnabled = true;
            ViewModel.MainWindow.textBoxLoopEnd.IsEnabled = true;
            ViewModel.MainWindow.toggleSwitchLoopTime.IsEnabled = true;
            sliderProgress.IsEnabled = true;

            await ViewModel.Play();

            ViewModel.position = player.NaturalDuration.Value;
            sliderProgress.Minimum = 0;
            sliderProgress.Maximum = ViewModel.position.TotalSeconds;

            ViewModel.EnablePlayPause();
            ViewModel.EnableStop();

            ViewModel.ProgressTimer.Start();
        }

        private void SliderProgress_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (player.Source is null || !player.IsOpen || MediaPlayerUserControlViewModel.ChangingVolume)
                return;

            MediaPlayerUserControlViewModel.ChangingProgress = true;
            ViewModel.Seeking = true;
            ViewModel.ProgressTimer.Stop();
            player.Pause();
        }

        private async void SliderProgress_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (player.Source is null || !player.IsOpen) return;

            MediaPlayerUserControlViewModel.ChangingProgress = false;
            int pos = Convert.ToInt32(sliderProgress.Value);
            await ViewModel.Seek(new TimeSpan(0, 0, 0, pos, 0));

            Focus();
        }

        private void SliderProgress_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (player.Source is null || !player.IsOpen) return;

            Point point = e.GetPosition(sliderProgress);
            ViewModel.SetLoopValue(point);
        }

        private void SliderProgress_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (ViewModel.Seeking)
            {
                int pos = Convert.ToInt32(sliderProgress.Value);
                player.Seek(new TimeSpan(0, 0, 0, pos, 0));
            }

            ViewModel.ShowTime();
        }

        private void SliderVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsLoaded) return;

            player.Volume = sliderVolume.Value;

            if (sliderVolume.Value == 0)
            {
                iconVolume.Kind = PackIconKind.VolumeMute;
            }
            else if (sliderVolume.Value <= .3)
            {
                iconVolume.Kind = PackIconKind.VolumeLow;
            }
            else if (sliderVolume.Value <= .6)
            {
                iconVolume.Kind = PackIconKind.VolumeMedium;
            }
            else
            {
                iconVolume.Kind = PackIconKind.VolumeHigh;
            }
        }

        private void SliderVolume_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ViewModel.OldVolume = sliderVolume.Value;
        }

        private async void SliderVolume_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sliderVolume.Value > 0)
            {
                ViewModel.OldVolume = sliderVolume.Value;
            }

            await Settings.CurrentSettings.Save();
        }

        private async void ButtonMuteUnmute_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.MuteToggle();
        }

        private void CheckBoxLoop_Checked(object sender, RoutedEventArgs e)
        {
            ViewModel.LoopVideo = true;
            Focus();
        }

        private void CheckBoxLoop_Unchecked(object sender, RoutedEventArgs e)
        {
            ViewModel.LoopVideo = false;
            Focus();
        }

        private void TextBoxLoop_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!IsLoaded) return;

            ViewModel.SetLoopTime(ViewModel.MainWindow.textBoxLoopStart.Text, ViewModel.MainWindow.textBoxLoopEnd.Text);
        }

        private void CheckBoxLoopTime_Checked(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;

            ViewModel.SetLoopTime(ViewModel.MainWindow.textBoxLoopStart.Text, ViewModel.MainWindow.textBoxLoopEnd.Text);
            ViewModel.LoopSpecificTime = true;
        }

        private void CheckBoxLoopTime_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;

            ViewModel.LoopSpecificTime = false;
            ViewModel.SetSelection(0, 0);
        }

        private async void MediaFile_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                if (Path.GetExtension(files[0]) == ".playlist")
                {
                    string name = Path.GetFileNameWithoutExtension(files[0]);
                    string[] filePaths = await File.ReadAllLinesAsync(files[0]);
                    Playlist playlist = ViewModel.GetPlaylist(name, filePaths);
                    ViewModel.MainWindow.ViewModel.SelectedPlaylist = playlist;

                    await ViewModel.MainWindow.ViewModel.AddMediasToQueue(playlist.Medias);
                }
                else
                {
                    List<Media> medias = new List<Media>();
                    files.ToList().ForEach(x => medias.Add(new Media(x)));

                    await ViewModel.MainWindow.ViewModel.AddMediasToQueue(medias);
                }
            }
        }

        private void NumericPlaybackSpeed_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            if (!IsLoaded || !e.NewValue.HasValue) return;

            ViewModel.ChangeSpeed(e.NewValue.Value);
        }

        private void ButtonQueue_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.MainWindow.ViewModel.OpenQueue();
        }

        private async void DataGridQueue_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int index = ViewModel.MainWindow.dataGridQueue.SelectedIndex;
            if (index == -1)
                return;

            await ViewModel.ChangeTrack(index);
        }

        private void ButtonSkipForward_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.MainWindow.dataGridQueue.SelectedIndex++;
        }

        private async void ButtonSkipBackwards_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.PreviousTrack();
        }

        private void ButtonResetLoop_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ResetLoop();
        }

        private void ButtonPlaylists_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.MainWindow.ViewModel.OpenPlaylists();
        }

        private async void GridMediaElementBackground_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!ViewModel.DoubleClickTimer.IsEnabled)
            {
                ViewModel.DoubleClickTimer.Start();
                await ViewModel.PlayPause();
            }
            else
            {
                if (ViewModel.IsFullscreen)
                    ViewModel.ExitFullscreen();
                else
                    ViewModel.EnterFullscreen();

                await ViewModel.PlayPause();
            }
        }

        private void UserControl_MouseMove(object sender, MouseEventArgs e)
        {
            if (!ViewModel.IsFullscreen)
                return;

            ViewModel.ShowControlsInFullscreen();
        }

        private void ButtonFullscreen_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.IsFullscreen)
                ViewModel.ExitFullscreen();
            else
                ViewModel.EnterFullscreen();
        }

        private void GridControls_MouseEnter(object sender, MouseEventArgs e)
        {
            ViewModel.MouseOverControls = true;
        }

        private void GridControls_MouseLeave(object sender, MouseEventArgs e)
        {
            ViewModel.MouseOverControls = false;
        }
    }
}
