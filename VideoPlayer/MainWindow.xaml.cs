using MahApps.Metro;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
        public Settings Settings;

        public static RoutedCommand PlayPauseCommand = new RoutedCommand();
        public static RoutedCommand SkipForwardCommand = new RoutedCommand();
        public static RoutedCommand SkipBackwardsCommand = new RoutedCommand();

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

            ViewModel.ChangeTheme(comboBoxTheme.SelectedItem.ToString(), comboBoxColor.SelectedItem as ColorScheme).ConfigureAwait(false);

            Task.Run(async () =>
            {
                ICollection<Playlist> playlists = await ViewModel.GetPlaylists();
                playlists.ToList().ForEach(x => ViewModel.Playlists.Add(x));
            });

            string[] cmdLine = Environment.GetCommandLineArgs();
            List<string> filePaths = cmdLine.Where(x => validExtensions.Contains(Path.GetExtension(x))).ToList();
            if (filePaths.Count > 0)
            {
                List<Media> medias = new List<Media>();
                filePaths.ForEach(x => medias.Add(new Media(x)));
                ViewModel.AddMediasToQueue(medias).ConfigureAwait(false);
            }

            PlayPauseCommand.InputGestures.Add(new KeyGesture(Key.Space));
            SkipForwardCommand.InputGestures.Add(new KeyGesture(Key.Right));
            SkipBackwardsCommand.InputGestures.Add(new KeyGesture(Key.Left));
        }

        private async void PlayPause_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            await ViewModel.UserControl.ViewModel.PlayPause();
        }

        private async void SkipForward_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            await ViewModel.UserControl.ViewModel.SkipForward(5);
        }

        private async void SkipBackwards_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            await ViewModel.UserControl.ViewModel.SkipBackwards(5);
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

        private async void ComboBoxThemeSettings_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;

            await ViewModel.ChangeTheme(comboBoxTheme.SelectedItem.ToString(), comboBoxColor.SelectedItem as ColorScheme);
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
                menuItemPlaylistsEditMedias.IsEnabled = false;
                menuItemPlaylistsAddToQueue.IsEnabled = false;
            }
            else
            {
                menuItemPlaylistsRemove.IsEnabled = true;
                menuItemPlaylistsEditMedias.IsEnabled = true;
                menuItemPlaylistsAddToQueue.IsEnabled = true;
            }
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

        private async void MenuItemPlaylistsRemove_Click(object sender, RoutedEventArgs e)
        {
            MessageDialogResult result = await this.ShowMessageAsync("Delete playlist", $"Are you sure that you want to delete the playlist '{ViewModel.SelectedPlaylist.Name}'?", MessageDialogStyle.AffirmativeAndNegative);
            if (result == MessageDialogResult.Affirmative)
                await ViewModel.RemovePlaylist();
        }

        private void MenuItemPlaylistsEditMedias_Click(object sender, RoutedEventArgs e)
        {
            flyoutPlaylist.IsOpen = true;
        }

        private async void ButtonCreateNewPlaylist_Click(object sender, RoutedEventArgs e)
        {
            string name = await this.ShowInputAsync("Create playlist", "Please write the name of the playlist");

            if (name != null)
            {
                Playlist playlist = new Playlist(name);
                ViewModel.Playlists.Add(playlist);
                await ViewModel.SavePlaylists();
            }
        }

        private void FlyoutPlaylist_IsOpenChanged(object sender, RoutedEventArgs e)
        {
            flyoutPlaylists.IsPinned = flyoutPlaylist.IsOpen;
            if (!flyoutPlaylist.IsOpen)
                dataGridPlaylist.SelectedItem = null;
        }

        private void DataGridPlaylistContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            if (ViewModel.SelectedPlaylistMedia is null)
                menuItemPlaylistRemove.IsEnabled = false;
            else
                menuItemPlaylistRemove.IsEnabled = true;
        }

        private async void MenuItemPlaylistRemove_Click(object sender, RoutedEventArgs e)
        {
            MessageDialogResult result = await this.ShowMessageAsync("Remove media", $"Are you sure that you want to remove the media '{ViewModel.SelectedPlaylistMedia.Name}' from the playlist '{ViewModel.SelectedPlaylist.Name}'?", MessageDialogStyle.AffirmativeAndNegative);
            if (result == MessageDialogResult.Affirmative)
            {
                ViewModel.SelectedPlaylist.Medias.Remove(ViewModel.SelectedPlaylistMedia);
                await ViewModel.SavePlaylists();
            }
        }

        private async void DataGridPlaylists_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ViewModel.SelectedPlaylist != null)
                await ViewModel.ChangePlaylist();
        }

        private async void MenuItemPlaylistsAddToQueue_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.SelectedPlaylist != null)
                await ViewModel.AddMediasToQueue(ViewModel.SelectedPlaylist.Medias);
        }

        private void DataGridPlaylists_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ViewModel.PlaylistsRowIndex = ViewModel.GetCurrentRowIndex(dataGridPlaylists, e.GetPosition);
            if (ViewModel.PlaylistsRowIndex < 0)
                return;
            dataGridPlaylists.SelectedIndex = ViewModel.PlaylistsRowIndex;
            if (!(dataGridPlaylists.Items[ViewModel.PlaylistsRowIndex] is Playlist selectedPlaylist))
                return;
            if (DragDrop.DoDragDrop(dataGridPlaylists, selectedPlaylist, DragDropEffects.Move) != DragDropEffects.None)
                dataGridPlaylists.SelectedItem = selectedPlaylist;
        }

        private async void DataGridPlaylists_Drop(object sender, DragEventArgs e)
        {
            if (ViewModel.PlaylistRowIndex < 0)
                return;
            int index = ViewModel.GetCurrentRowIndex(dataGridPlaylists, e.GetPosition);
            if (index < 0 || index == ViewModel.PlaylistsRowIndex)
                return;

            Playlist changedPlaylist = ViewModel.Playlists[ViewModel.PlaylistsRowIndex];
            ViewModel.Playlists.RemoveAt(ViewModel.PlaylistsRowIndex);
            ViewModel.Playlists.Insert(index, changedPlaylist);
            await ViewModel.SavePlaylists();
        }

        private void DataGridPlaylist_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ViewModel.PlaylistRowIndex = ViewModel.GetCurrentRowIndex(dataGridPlaylist, e.GetPosition);
            if (ViewModel.PlaylistRowIndex < 0)
                return;
            dataGridPlaylist.SelectedIndex = ViewModel.PlaylistRowIndex;
            if (!(dataGridPlaylist.Items[ViewModel.PlaylistRowIndex] is Media selectedMedia))
                return;
            if (DragDrop.DoDragDrop(dataGridPlaylist, selectedMedia, DragDropEffects.Move) != DragDropEffects.None)
                dataGridPlaylist.SelectedItem = selectedMedia;
        }

        private async void DataGridPlaylist_Drop(object sender, DragEventArgs e)
        {
            if (ViewModel.PlaylistRowIndex < 0)
                return;
            int index = ViewModel.GetCurrentRowIndex(dataGridPlaylist, e.GetPosition);
            if (index < 0 || index == ViewModel.PlaylistRowIndex)
                return;

            Media changedMedia = ViewModel.SelectedPlaylist.Medias[ViewModel.PlaylistRowIndex];
            ViewModel.SelectedPlaylist.Medias.RemoveAt(ViewModel.PlaylistRowIndex);
            ViewModel.SelectedPlaylist.Medias.Insert(index, changedMedia);
            await ViewModel.SavePlaylists();
        }

        private void DataGridQueue_Drop(object sender, DragEventArgs e)
        {
            if (ViewModel.QueueRowIndex < 0)
                return;
            int index = ViewModel.GetCurrentRowIndex(dataGridQueue, e.GetPosition);
            if (index < 0 || index == ViewModel.QueueRowIndex)
                return;

            Media changedMedia = ViewModel.Queue[ViewModel.QueueRowIndex];
            ViewModel.Queue.RemoveAt(ViewModel.QueueRowIndex);
            ViewModel.Queue.Insert(index, changedMedia);
        }

        private void DataGridQueue_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        private void DataGridQueue_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            ViewModel.QueueRowIndex = ViewModel.GetCurrentRowIndex(dataGridQueue, e.GetPosition);
            if (ViewModel.QueueRowIndex < 0)
                return;
            if (!(dataGridQueue.Items[ViewModel.QueueRowIndex] is Media selectedMedia))
                return;
            DragDrop.DoDragDrop(dataGridQueue, selectedMedia, DragDropEffects.Move);
        }

        private void FlyoutPlaylists_IsOpenChanged(object sender, RoutedEventArgs e)
        {
            if (!flyoutPlaylists.IsOpen)
                dataGridPlaylists.SelectedItem = null;
        }

        private void DataGridPlaylists_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            int index = ViewModel.GetCurrentRowIndex(dataGridPlaylists, e.GetPosition);
            if (index < 0)
                return;
            dataGridPlaylists.SelectedIndex = index;
        }

        private async void MenuItemPlaylistsEditName_Click(object sender, RoutedEventArgs e)
        {
            string name = await this.ShowInputAsync("Edit playlist name", $"Please enter a new name for the playlist '{ViewModel.SelectedPlaylist.Name}'");
            if (string.IsNullOrWhiteSpace(name))
                return;

            ViewModel.SelectedPlaylist.Name = name;
            await ViewModel.SavePlaylists();
        }
    }
}
