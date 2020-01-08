using MahApps.Metro;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;
using VideoPlayer.Entities;
using VideoPlayer.UserControls;
using VideoPlayer.ViewModels;

namespace VideoPlayer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public MainWindowViewModel ViewModel;

        public static Process FFmpegProcess;

        private readonly string[] validExtensions = new string[]
        {
            ".mpg",
            ".avi",
            ".wma",
            ".mov",
            ".wav",
            ".mp2",
            ".mp3",
            ".mp4"
        };

        public MainWindow()
        {
            InitializeComponent();
            ViewModel = new MainWindowViewModel(this);
            DataContext = ViewModel;

            ViewModel.UserControl = new MediaPlayerUserControl(this);
            masterUserControl.Content = ViewModel.UserControl;

            string[] cmdLine = Environment.GetCommandLineArgs();
            List<string> filePaths = cmdLine.Where(x => validExtensions.Contains(Path.GetExtension(x))).ToList();
            if (filePaths.Count > 0)
            {
                List<Media> medias = new List<Media>();
                filePaths.ForEach(x => medias.Add(new Media(x)));
                ViewModel.AddMediasToQueue(medias).ConfigureAwait(false);
            }
        }

        private void ButtonWindowSettings_Click(object sender, RoutedEventArgs e)
        {
            flyoutCredits.IsOpen = false;
            flyoutSettings.IsOpen = !flyoutSettings.IsOpen;
        }

        private void ButtonWindowCredits_Click(object sender, RoutedEventArgs e)
        {
            flyoutSettings.IsOpen = false;
            flyoutCredits.IsOpen = !flyoutCredits.IsOpen;
        }

        private void MetroWindow_Closing(object sender, CancelEventArgs e)
        {
            ViewModel.Hotkeys.ForEach(x => x.Dispose());
        }

        private void NumericPlaybackSpeed_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            if (!IsLoaded || !e.NewValue.HasValue) return;

            ViewModel.UserControl.ViewModel.ChangeSpeed(e.NewValue.Value);
        }

        private void TextBoxLoop_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!IsLoaded) return;

            ViewModel.UserControl.ViewModel.SetLoopTime(textBoxLoopStart.Text, textBoxLoopEnd.Text);
        }

        private void ComboBoxThemeSettings_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;

            ViewModel.ChangeTheme(comboBoxTheme.SelectedItem.ToString(), comboBoxColor.SelectedItem as ColorScheme);
        }

        private async void DataGridQueue_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int index = dataGridQueue.SelectedIndex;
            if (index == -1)
                return;

            await ViewModel.UserControl.ViewModel.ChangeTrack(index);
        }

        private void ButtonClearQueue_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.Queue.Clear();
            ViewModel.UserControl.buttonSkipForward.IsEnabled = false;
        }

        private void DataGridQueueContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            if (ViewModel.SelectedMedia is null ||
                ViewModel.Queue.Count == 1 ||
                ((Media)dataGridQueue.SelectedItem) == ViewModel.SelectedMedia)
            {
                menuItemQueueRemove.IsEnabled = false;
            }
            else
            {
                menuItemQueueRemove.IsEnabled = true;
            }
        }

        private void MenuItemQueueRemove_Click(object sender, RoutedEventArgs e)
        {
            Media media = dataGridQueue.SelectedItem as Media;
            ViewModel.Queue.Remove(media);
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            ProcessStartInfo processStartInfo = new ProcessStartInfo(e.Uri.ToString())
            {
                UseShellExecute = true,
                Verb = "open"
            };
            Process.Start(processStartInfo);
        }

        private void ToggleSwitchLoop_Checked(object sender, RoutedEventArgs e)
        {
            ViewModel.UserControl.ViewModel.LoopVideo = true;
            Focus();
        }

        private void ToggleSwitchLoop_Unchecked(object sender, RoutedEventArgs e)
        {
            ViewModel.UserControl.ViewModel.LoopVideo = false;
            Focus();
        }

        private void ToggleSwitchLoopTime_Checked(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;

            ViewModel.UserControl.ViewModel.SetLoopTime(textBoxLoopStart.Text, textBoxLoopEnd.Text);
            ViewModel.UserControl.ViewModel.LoopSpecificTime = true;
        }

        private void ToggleSwitchLoopTime_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;

            ViewModel.UserControl.ViewModel.LoopSpecificTime = false;
            ViewModel.UserControl.ViewModel.SetSelection(0, 0);
        }

        private async void GridFlyoutQueue_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                if (Path.GetExtension(files[0]) == ".playlist")
                {
                    string name = Path.GetFileNameWithoutExtension(files[0]);
                    string[] filePaths = await File.ReadAllLinesAsync(files[0]);
                    Playlist playlist = ViewModel.UserControl.ViewModel.GetPlaylist(name, filePaths);
                    ViewModel.SelectedPlaylist = playlist;

                    await ViewModel.AddMediasToQueue(playlist.Medias);
                }
                else
                {
                    List<Media> medias = new List<Media>();
                    files.ToList().ForEach(x => medias.Add(new Media(x)));

                    await ViewModel.AddMediasToQueue(medias);
                }
            }
        }

        private void DataGridPlaylistsContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            if (ViewModel.SelectedPlaylist is null)
            {
                menuItemPlaylistsRemove.IsEnabled = false;
                menuItemPlaylistsEdit.IsEnabled = false;
            }
            else
            {
                menuItemPlaylistsRemove.IsEnabled = true;
                menuItemPlaylistsEdit.IsEnabled = true;
            }
        }

        private async void DataGridPlaylists_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            await ViewModel.ChangePlaylist();
        }

        private void ButtonAddMediasToPlaylist_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Select media file(s)",
                DefaultExt = ".avi",
                Filter = "Media Files|*.mpg;*.avi;*.wma;*.mov;*.wav;*.mp2;*.mp3;*.mp4|All Files|*.*",
                Multiselect = true
            };

            if (openFileDialog.ShowDialog() == true)
            {
                List<string> fileNames = openFileDialog.FileNames.ToList();
                List<Media> medias = new List<Media>();
                fileNames.ForEach(x => medias.Add(new Media(x)));
                ViewModel.AddMediasToPlaylist(medias);
            }
        }

        private void MenuItemPlaylistsRemove_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.RemovePlaylist();
        }

        private void MenuItemPlaylistsEdit_Click(object sender, RoutedEventArgs e)
        {
            flyoutPlaylist.IsOpen = true;
        }

        private async void ButtonAddNewPlaylist_Click(object sender, RoutedEventArgs e)
        {
            string name = await this.ShowInputAsync("Create playlist", "Please write the name of the playlist");

            if (name != null)
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

                    Playlist playlist = new Playlist(medias, name);
                    var playlistSave = await playlist.Save();

                    if (!playlistSave.isValid)
                        await this.ShowMessageAsync("Error saving playlist", playlistSave.message);
                    else
                    {
                        ViewModel.Playlists.Add(playlist);
                        await this.ShowMessageAsync("Playlist saved", $"The playlist {playlist.Name} has been saved");
                    }
                }
            }
        }

        private void FlyoutPlaylist_IsOpenChanged(object sender, RoutedEventArgs e)
        {
            flyoutPlaylists.IsPinned = flyoutPlaylist.IsOpen;
        }

        private void DataGridPlaylists_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            dataGridPlaylistsContextMenu.IsOpen = true;
            e.Handled = true;
        }
    }
}
