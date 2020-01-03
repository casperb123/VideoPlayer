using Microsoft.Win32;
using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Unosquare.FFME.Common;
using VideoPlayer.ViewModels;
using MaterialDesignThemes.Wpf;
using VideoPlayer.Entities;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace VideoPlayer.UserControls
{
    /// <summary>
    /// Interaction logic for MediaPlayer.xaml
    /// </summary>F
    public partial class MediaPlayerUserControl : UserControl
    {
        private readonly MediaPlayerUserControlViewModel viewModel;

        public static RoutedUICommand PlayPauseCmd;
        public static RoutedUICommand SkipForwardCmd;
        public static RoutedUICommand SkipBackwardCmd;

        public MediaPlayerUserControl(string filePath = null)
        {
            PlayPauseCmd = new RoutedUICommand("Toggle playing", "PlayPause", typeof(MediaPlayerUserControl));
            SkipForwardCmd = new RoutedUICommand("Skip forward", "SkipForward", typeof(MediaPlayerUserControl));
            SkipBackwardCmd = new RoutedUICommand("Skip backwards", "SkipBackard", typeof(MediaPlayerUserControl));

            InitializeComponent();

            sliderProgress.AddHandler(PreviewMouseLeftButtonDownEvent, new MouseButtonEventHandler(SliderProgress_PreviewMouseLeftButtonDown), true);
            sliderProgress.AddHandler(PreviewMouseLeftButtonUpEvent, new MouseButtonEventHandler(SliderProgress_PreviewMouseLeftButtonUp), true);

            if (filePath is null)
            {
                viewModel = new MediaPlayerUserControlViewModel(this);
            }
            else
            {
                Media media = new Media(filePath);
                viewModel = new MediaPlayerUserControlViewModel(this, media);
            }
            DataContext = viewModel;
        }

        private void CommandCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
            e.Handled = true;
        }

        private async void PlayPauseExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (player.Source != null)
            {
                if (player.IsPlaying)
                {
                    await viewModel.Pause();
                }
                else
                {
                    await viewModel.Play();
                }
            }
        }

        private void SkipForwardExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            int pos = Convert.ToInt32(sliderProgress.Value + 5);
            player.Position = new TimeSpan(0, 0, 0, pos, 0);
            sliderProgress.Value = player.Position.TotalSeconds;
        }

        private void SkipBackwardExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            int pos = Convert.ToInt32(sliderProgress.Value - 5);
            player.Position = new TimeSpan(0, 0, 0, pos, 0);
            sliderProgress.Value = player.Position.TotalSeconds;
        }

        private void ButtonPlayPause_Click(object sender, RoutedEventArgs e)
        {
            PlayPauseCmd.Execute(null, sender as Button);
            Focus();
        }

        private async void ButtonStop_Click(object sender, RoutedEventArgs e)
        {
            await viewModel.Stop();
            Focus();
        }

        private async void ButtonAddToQueue_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Select video file",
                DefaultExt = ".avi",
                Filter = "Media Files|*.mpg;*.avi;*.wma;*.mov;*.wav;*.mp2;*.mp3;*.mp4|All Files|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                Media media = new Media(openFileDialog.FileName);

                if (player.IsOpen || viewModel.Queue.Count >= 1)
                {
                    viewModel.AddToQueue(media);
                }
                else
                {
                    await viewModel.Open(media);
                    viewModel.SelectedMedia = media;
                }
            }

            Focus();
        }

        private async void Player_MediaEnded(object sender, EventArgs e)
        {
            if (viewModel.LoopVideo)
            {
                await viewModel.Stop(false);
                await viewModel.Play();
            }
            else
            {
                if (viewModel.Queue.Count > 0)
                {
                    listBoxQueue.SelectedIndex++;
                }
                else
                {
                    await viewModel.Stop();
                }
            }
        }

        private async void Player_MediaOpened(object sender, MediaOpenedEventArgs e)
        {
            textBoxLoopStart.IsEnabled = true;
            textBoxLoopEnd.IsEnabled = true;
            checkBoxLoopTime.IsEnabled = true;
            sliderProgress.IsEnabled = true;

            await viewModel.Play();

            viewModel.position = player.NaturalDuration.Value;
            sliderProgress.Minimum = 0;
            sliderProgress.Maximum = viewModel.position.TotalSeconds;

            viewModel.EnablePlayPause();
            viewModel.EnableStop();

            viewModel.ProgressTimer.Start();
        }

        private void SliderProgress_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (player.Source is null || !player.IsOpen) return;
            viewModel.Seeking = true;
            viewModel.ProgressTimer.Stop();
            player.Pause();
        }

        private async void SliderProgress_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (player.Source is null || !player.IsOpen) return;

            int pos = Convert.ToInt32(sliderProgress.Value);
            await viewModel.Seek(new TimeSpan(0, 0, 0, pos, 0));

            Focus();
        }

        private void SliderProgress_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (player.Source is null || !player.IsOpen) return;

            Point point = e.GetPosition(sliderProgress);
            viewModel.SetLoopValue(point);
        }

        private void SliderProgress_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (viewModel.Seeking)
            {
                int pos = Convert.ToInt32(sliderProgress.Value);
                player.Seek(new TimeSpan(0, 0, 0, pos, 0));
            }

            viewModel.ShowTime();
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
            viewModel.OldVolume = sliderVolume.Value;
        }

        private void SliderVolume_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sliderVolume.Value > 0)
            {
                viewModel.OldVolume = sliderVolume.Value;
            }

            Focus();
        }

        private void ButtonMuteUnmute_Click(object sender, RoutedEventArgs e)
        {
            viewModel.MuteToggle();
            Focus();
        }

        private void ButtonSettings_Click(object sender, RoutedEventArgs e)
        {
            viewModel.ToggleSettings();
            Focus();
        }

        private void CheckBoxLoop_Checked(object sender, RoutedEventArgs e)
        {
            viewModel.LoopVideo = true;
            Focus();
        }

        private void CheckBoxLoop_Unchecked(object sender, RoutedEventArgs e)
        {
            viewModel.LoopVideo = false;
            Focus();
        }

        private void TextBoxLoop_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!IsLoaded) return;

            viewModel.SetLoopTime(textBoxLoopStart.Text, textBoxLoopEnd.Text);
        }

        private void CheckBoxLoopTime_Checked(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;

            viewModel.SetLoopTime(textBoxLoopStart.Text, textBoxLoopEnd.Text);
            viewModel.LoopSpecificTime = true;
        }

        private void CheckBoxLoopTime_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;

            viewModel.LoopSpecificTime = false;
            viewModel.SetSelection(0, 0);
        }

        private void HyperLinkResetLoop_Click(object sender, RoutedEventArgs e)
        {
            viewModel.ResetLoop();
        }

        private void GridMediaElementBackground_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length > 1)
                {
                    MessageBox.Show("Please only drag 1 media file", "Video Player", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                Media media = new Media(files[0]);
                viewModel.AddToQueue(media);
            }
        }

        private async void Player_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (player.Source is null || !player.IsOpen) return;
            if (player.IsPlaying)
            {
                await viewModel.Stop();
            }
            else
            {
                await viewModel.Play();
            }
        }

        private void NumericPlaybackSpeed_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            if (!IsLoaded || !e.NewValue.HasValue) return;

            viewModel.ChangeSpeed(e.NewValue.Value);
        }

        private void ButtonQueue_Click(object sender, RoutedEventArgs e)
        {
            viewModel.ToggleQueuePanel();
        }

        private void ListBoxQueue_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int index = listBoxQueue.SelectedIndex;
            if (index == -1)
                return;

            viewModel.ChangeTrack(index);
        }

        private void ButtonClearQueue_Click(object sender, RoutedEventArgs e)
        {
            viewModel.Queue.Clear();
            buttonSkipForward.IsEnabled = false;
        }

        private void MenuItemQueueRemove_Click(object sender, RoutedEventArgs e)
        {
            Media media = listBoxQueue.SelectedItem as Media;
            viewModel.Queue.Remove(media);
        }

        private void ListBoxQueueContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            if (viewModel.SelectedMedia is null ||
                viewModel.Queue.Count == 1 ||
                ((Media)listBoxQueue.SelectedItem) == viewModel.SelectedMedia)
            {
                menuItemQueueRemove.IsEnabled = false;
            }
            else
            {
                menuItemQueueRemove.IsEnabled = true;
            }
        }

        private void ButtonSkipForward_Click(object sender, RoutedEventArgs e)
        {
            listBoxQueue.SelectedIndex++;
        }

        private async void ButtonSkipBackwards_Click(object sender, RoutedEventArgs e)
        {
            await viewModel.SkipBackwards();
        }
    }
}
