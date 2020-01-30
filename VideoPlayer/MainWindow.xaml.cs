using MahApps.Metro;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
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
        private ProgressDialogController progressDialog;

        public MainWindowViewModel ViewModel;

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

        //private async void Updater_InstallationFailed(object sender, ExceptionEventArgs<Exception> e)
        //{
        //    updater.Dispose();
        //    await progressDialog.CloseAsync();
        //    //await this.ShowMessageAsync("Updating application", "Updating the application has failed!");
        //    await this.ShowMessageAsync("Updating failed", $"Updating the application has failed." +
        //                                                   $"\n\n{e.Message}");
        //}

        //private async void Updater_InstallationCompleted(object sender, EventArgs e)
        //{
        //    await progressDialog.CloseAsync();
        //    MessageDialogResult result = await this.ShowMessageAsync("Update completed", "The update has been installed successfully. Would you like to restart the application?", MessageDialogStyle.AffirmativeAndNegative);
        //    if (result == MessageDialogResult.Affirmative)
        //    {
        //        System.Windows.Forms.Application.Restart();
        //        //Process.Start(updater.OriginalInstallPath);
        //        //Close();
        //    }
        //}

        //private void Updater_InstallationStarted(object sender, EventArgs e)
        //{
        //    progressDialog.SetMessage("Installing update...");
        //}

        //private void Updater_DownloadingCompleted(object sender, EventArgs e)
        //{
        //    updater.InstallUpdate();
        //}

        //private void Updater_DownloadingProgressed(object sender, DownloadProgressEventArgs e)
        //{
        //    progressDialog.SetMessage($"Downloading update: {e.BytesReceived / 1000}/{e.TotalBytesToReceive / 1000} kb");
        //    progressDialog.SetProgress(e.ProgressPercent);
        //}

        //private async void Updater_DownloadingStarted(object sender, EventArgs e)
        //{
        //    progressDialog = await this.ShowProgressAsync("Updating application", $"Starting download...");
        //    progressDialog.Minimum = 0;
        //    progressDialog.Maximum = 100;
        //}

        //public async void Updater_UpdateAvailable(object sender, VersionEventArgs args)
        //{
        //    if (!Settings.CurrentSettings.NotifyUpdates)
        //    {
        //        UpdateAvailable = true;
        //        buttonUpdate.Content = "Update available";
        //        return;
        //    }

        //    MessageDialogResult result = await this.ShowMessageAsync($"Update available", $"An update is available. Current version: '{args.CurrentVersion.ToString()}' New version: '{args.NewVersion.ToString()}'" +
        //                                                                                  $"\n\nWould you like to update now?", MessageDialogStyle.AffirmativeAndNegative);

        //    if (result == MessageDialogResult.Affirmative)
        //        await updater.DownloadUpdateAsync();
        //    else
        //    {
        //        UpdateAvailable = true;
        //        buttonUpdate.Content = "Update available";
        //        Settings.CurrentSettings.NotifyUpdates = false;
        //        await Settings.CurrentSettings.Save();
        //    }
        //}

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

        private async void MetroWindow_Closing(object sender, CancelEventArgs e)
        {
            ViewModel.Hotkeys.ForEach(x => x.Dispose());
            await Settings.CurrentSettings.Save();
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

            string theme = comboBoxTheme.SelectedItem.ToString();
            ColorScheme color = comboBoxColor.SelectedItem as ColorScheme;
            ThemeManager.ChangeTheme(Application.Current, theme, color.Name);
            ViewModel.SettingsChanged = true;
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
            if (ViewModel.Queue.Count < 1 || !ViewModel.QueueMediaSelected)
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
            Media media = ViewModel.Queue[ViewModel.QueueRowIndex];
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
                List<Media> medias = new List<Media>();
                files.ToList().Where(x => validExtensions.Contains(Path.GetExtension(x))).ToList().ForEach(x => medias.Add(new Media(x)));

                await ViewModel.AddMediasToQueue(medias);
            }
        }

        private void DataGridPlaylistsContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            if (ViewModel.SelectedPlaylist is null)
            {
                menuItemPlaylistsRemove.IsEnabled = false;
                menuItemPlaylistsEditMedias.IsEnabled = false;
                menuItemPlaylistsAddToQueue.IsEnabled = false;
                menuItemPlaylistsEditName.IsEnabled = false;
            }
            else
            {
                menuItemPlaylistsRemove.IsEnabled = true;
                menuItemPlaylistsEditMedias.IsEnabled = true;
                menuItemPlaylistsAddToQueue.IsEnabled = true;
                menuItemPlaylistsEditName.IsEnabled = true;
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
            if (!(dataGridPlaylists.Items[ViewModel.PlaylistsRowIndex] is Playlist selectedPlaylist))
                return;
            ViewModel.PlaylistsPlaylistSelected = true;
            DragDrop.DoDragDrop(dataGridPlaylists, selectedPlaylist, DragDropEffects.Move);
            if (ViewModel.PlaylistsPlaylistSelected)
                dataGridPlaylists.SelectedIndex = ViewModel.PlaylistsRowIndex;
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
            ViewModel.PlaylistsPlaylistSelected = false;
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
            ViewModel.QueueMediaSelected = false;
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

        private async void ButtonUpdate_Click(object sender, RoutedEventArgs e)
        {
            bool update = await ViewModel.Updater.CheckForUpdateAsync();

            if (!update)
                await this.ShowMessageAsync("Up to date", "You're already using the latest version of the application");
        }

        private void Flyout_MouseEnter(object sender, MouseEventArgs e)
        {
            if (ViewModel.UserControl.ViewModel.IsFullscreen)
            {
                ViewModel.UserControl.ViewModel.ControlsTimer.Stop();
                ViewModel.UserControl.gridControls.IsEnabled = true;
                ViewModel.UserControl.gridControls.Visibility = Visibility.Visible;
                Mouse.OverrideCursor = null;
            }
        }

        private void Flyout_MouseLeave(object sender, MouseEventArgs e)
        {
            if (ViewModel.UserControl.ViewModel.IsFullscreen)
                ViewModel.UserControl.ViewModel.ControlsTimer.Start();
        }

        private async void FlyoutSettings_IsOpenChanged(object sender, RoutedEventArgs e)
        {
            if (ViewModel.SettingsChanged && !flyoutSettings.IsOpen)
            {
                await Settings.CurrentSettings.Save();
                ViewModel.SettingsChanged = false;
            }
        }

        private void ToggleSwitchCheckForUpdates_IsCheckedChanged(object sender, EventArgs e)
        {
            ViewModel.SettingsChanged = true;
        }

        private void DataGridQueue_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ViewModel.QueueRowIndex = ViewModel.GetCurrentRowIndex(dataGridQueue, e.GetPosition);
            if (ViewModel.QueueRowIndex < 0)
                return;
            if (!(dataGridQueue.Items[ViewModel.QueueRowIndex] is Media selectedMedia))
                return;
            ViewModel.QueueMediaSelected = true;
            DragDrop.DoDragDrop(dataGridQueue, selectedMedia, DragDropEffects.Move);
            if (ViewModel.QueueMediaSelected)
                dataGridQueue.SelectedIndex = ViewModel.QueueRowIndex;
        }

        private async void DataGridPlaylists_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ViewModel.SelectedPlaylist != null)
                await ViewModel.ChangePlaylist();
        }
    }
}
