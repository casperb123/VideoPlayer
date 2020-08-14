using MahApps.Metro.Controls.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using VideoPlayer.Entities;
using VideoPlayer.UserControls;
using Application = System.Windows.Application;
using MahApps.Metro.Controls;
using ControlzEx.Theming;
using GitHubUpdater;
using Version = GitHubUpdater.Version;
using VideoPlayer.Properties;

namespace VideoPlayer.ViewModels
{
    public class MainWindowViewModel : Protected, INotifyPropertyChanged
    {
        private readonly MainWindow mainWindow;
        private ObservableCollection<Media> queue;
        private ObservableCollection<Media> oldQueue;
        private Media selectedMedia;
        private ObservableCollection<Playlist> playlists;
        private Playlist selectedPlaylist;
        private Media selectedPlaylistMedia;
        private ProgressDialogController progressDialog;

        public MediaPlayerUserControl UserControl;
        public List<Hotkey> Hotkeys;
        public int PlaylistsRowIndex;
        public int PlaylistRowIndex;
        public int QueueRowIndex;
        public bool QueueMediaSelected;
        public bool PlaylistsPlaylistSelected;
        public bool PlaylistMediaSelected;
        public bool SettingsChanged;
        public bool UpdateAvailable = false;
        public bool SettingsOpenedWithEdgeDetection;
        public bool QueueOpenedWithEdgeDetection;
        public bool PlaylistsOpenedWithEdgeDetection;
        public bool IsAnyContextOpen;
        public bool PlaylistsChanged;
        public readonly SoundProcessorViewModel SoundProcessor;
        public bool UpdateDownloaded;
        public readonly Updater Updater;

        public delegate Point GetPosition(IInputElement element);
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
                    throw new NullReferenceException("The old queue can't be null");

                oldQueue = value;
                OnPropertyChanged(nameof(OldQueue));
            }
        }

        public ObservableCollection<Playlist> Playlists
        {
            get => playlists;
            set
            {
                if (value is null)
                    throw new NullReferenceException("The playlists can't be null");

                playlists = value;
                OnPropertyChanged(nameof(Playlists));
            }
        }

        public Playlist SelectedPlaylist
        {
            get => selectedPlaylist;
            set
            {
                selectedPlaylist = value;
                OnPropertyChanged(nameof(SelectedPlaylist));
            }
        }

        public Media SelectedPlaylistMedia
        {
            get => selectedPlaylistMedia;
            set
            {
                selectedPlaylistMedia = value;
                OnPropertyChanged(nameof(SelectedPlaylistMedia));
            }
        }

        public bool IsFlyoutOpen
        {
            get
            {
                return mainWindow.flyoutCredits.IsOpen ||
                       mainWindow.flyoutPlaylist.IsOpen ||
                       mainWindow.flyoutPlaylists.IsOpen ||
                       mainWindow.flyoutQueue.IsOpen ||
                       mainWindow.flyoutSettings.IsOpen;
            }
        }

        private void OnPropertyChanged(string prop)
        {
            if (prop != null)
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        public MainWindowViewModel(MainWindow mainWindow)
        {
            string currentDir = Directory.GetCurrentDirectory();
            string oldFile = $@"{currentDir}\VideoPlayer.exe.old";

            if (File.Exists(oldFile))
                File.Delete(oldFile);

            this.mainWindow = mainWindow;
            mainWindow.Topmost = Settings.CurrentSettings.AlwaysOnTop;
            queue = new ObservableCollection<Media>();
            oldQueue = new ObservableCollection<Media>();
            playlists = new ObservableCollection<Playlist>();
            SoundProcessor = new SoundProcessorViewModel();

            Hotkey nextTrackHotkey = new Hotkey(Key.MediaNextTrack, KeyModifier.None, OnHotkeyHandler);
            Hotkey previousTrackHotkey = new Hotkey(Key.MediaPreviousTrack, KeyModifier.None, OnHotkeyHandler);
            Hotkey mediaPlayPauseHotkey = new Hotkey(Key.MediaPlayPause, KeyModifier.None, OnHotkeyHandler);
            Hotkeys = new List<Hotkey>
            {
                nextTrackHotkey,
                previousTrackHotkey,
                mediaPlayPauseHotkey
            };

            try
            {
                Updater = new Updater("casperb123", "VideoPlayer", Resources.GitHubToken);
                Updater.UpdateAvailable += Updater_UpdateAvailable;
                Updater.DownloadingStarted += Updater_DownloadingStarted;
                Updater.DownloadingProgressed += Updater_DownloadingProgressed;
                Updater.DownloadingCompleted += Updater_DownloadingCompleted;
                Updater.InstallationFailed += Updater_InstallationFailed;

                CheckForUpdates();
            }
            catch (Exception e)
            {
                if (e.InnerException is null)
                    mainWindow.ShowMessageAsync("Checking for updates failed", $"There was an error while checking for updates.\n\n" +
                                                                               $"Error:\n" +
                                                                               $"{e.Message}").ConfigureAwait(false);
                else
                    mainWindow.ShowMessageAsync("Checking for updates failed", $"There was an error while checking for updates.\n\n" +
                                                                               $"Error:\n" +
                                                                               $"{e.InnerException.Message}").ConfigureAwait(false);
            }
        }

        private async void CheckForUpdates()
        {
            if (Settings.CurrentSettings.CheckForUpdates)
            {
                Version version = await Updater.CheckForUpdatesAsync();
                if (version.IsCurrentVersion)
                    Updater.DeleteUpdateFiles();
            }
            else if (Updater.IsUpdateDownloaded())
            {
                UpdateDownloaded = true;
                mainWindow.buttonUpdate.Content = "Update downloaded";
            }
            else
                Updater.DeleteUpdateFiles();
        }

        private async void Updater_InstallationFailed(object sender, ExceptionEventArgs<Exception> e)
        {
            await mainWindow.ShowMessageAsync("Installation failed", "Installing the update failed.\n\n" +
                                                                     "Error:\n" +
                                                                     $"{e.Message}");
        }

        private void Updater_DownloadingProgressed(object sender, DownloadProgressEventArgs e)
        {
            progressDialog.SetProgress(e.ProgressPercent);
            progressDialog.SetMessage($"Estimated time left: {e.TimeLeft} ({e.Downloaded} of {e.ToDownload} downloaded)\n" +
                                      $"Time spent: {e.TimeSpent}");
        }

        private async void Updater_DownloadingCompleted(object sender, VersionEventArgs e)
        {
            Settings.CurrentSettings.NotifyUpdates = true;
            await Settings.CurrentSettings.Save();
            await progressDialog.CloseAsync();
            await NotifyDownloaded(e.CurrentVersion, e.LatestVersion, e.Changelog);
        }

        private async void Updater_DownloadingStarted(object sender, DownloadStartedEventArgs e)
        {
            progressDialog = await mainWindow.ShowProgressAsync($"Downloading update - {e.Version}", "Estimated time left: 0 sec (0 kb of 0 kb downloaded)\n" +
                                                                                                                            "Time spent: 0 sec");
            progressDialog.Minimum = 0;
            progressDialog.Maximum = 100;
        }

        private async void Updater_UpdateAvailable(object sender, VersionEventArgs e)
        {
            if (e.UpdateDownloaded)
            {
                if (Settings.CurrentSettings.NotifyUpdates || UpdateDownloaded)
                    await NotifyDownloaded(e.CurrentVersion, e.LatestVersion, e.Changelog);
                else
                {
                    UpdateDownloaded = true;
                    mainWindow.buttonUpdate.Content = "Update downloaded";
                }
            }
            else
            {
                if (Settings.CurrentSettings.NotifyUpdates || UpdateAvailable)
                {
                    string message = $"An update is available, would you like to download it now?\n" +
                                     $"Current version: {e.CurrentVersion}\n" +
                                     $"Latest version: {e.LatestVersion}\n\n" +
                                     $"Changelog:\n" +
                                     $"{e.Changelog}";

                    MessageDialogResult result = await mainWindow.ShowMessageAsync("Update available", message, MessageDialogStyle.AffirmativeAndNegative);

                    if (result == MessageDialogResult.Affirmative)
                    {
                        Settings.CurrentSettings.NotifyUpdates = true;
                        await Settings.CurrentSettings.Save();

                        Updater.DownloadUpdate();
                    }
                    else
                    {
                        Settings.CurrentSettings.NotifyUpdates = false;
                        await Settings.CurrentSettings.Save();
                        UpdateAvailable = true;
                        mainWindow.buttonUpdate.Content = "Update available";
                    }
                }
                else
                {
                    if (Settings.CurrentSettings.NotifyUpdates)
                    {
                        Settings.CurrentSettings.NotifyUpdates = false;
                        await Settings.CurrentSettings.Save();
                    }

                    UpdateAvailable = true;
                    UpdateDownloaded = false;
                    mainWindow.buttonUpdate.Content = "Update available";
                }
            }
        }

        public async Task NotifyDownloaded(Version currentVersion, Version latestVersion, string changelog = null)
        {
            string message = $"An update has been downloaded. Would you like to install it now?\n\n" +
                             $"Current version: {currentVersion}\n" +
                             $"Downloaded version: {latestVersion}";

            if (changelog != null)
                message += $"\n\nChangelog:\n" +
                           $"{changelog}";

            MessageDialogResult result = await mainWindow.ShowMessageAsync("Update downloaded", message, MessageDialogStyle.AffirmativeAndNegative);

            if (result == MessageDialogResult.Affirmative)
            {
                Settings.CurrentSettings.NotifyUpdates = true;
                await Settings.CurrentSettings.Save();

                Updater.InstallUpdate();
            }
            else
            {
                Settings.CurrentSettings.NotifyUpdates = false;
                await Settings.CurrentSettings.Save();
                UpdateDownloaded = true;
                UpdateAvailable = false;

                mainWindow.buttonUpdate.Content = "Update downloaded";
            }
        }

        public async Task ChangeTheme(string theme, string color)
        {
            ThemeManager.Current.ChangeTheme(Application.Current, $"{theme}.{color}");
            await Settings.CurrentSettings.Save();
        }

        public async Task AddToQueue(Media media)
        {
            if (UserControl.player.IsOpen || Queue.Count >= 1)
            {
                Queue.Add(media);
                UserControl.buttonSkipForward.IsEnabled = true;
            }
            else
            {
                await UserControl.ViewModel.Open(media);
                SelectedMedia = media;
            }
        }

        public async Task AddMediasToQueue(ICollection<Media> medias)
        {
            foreach (Media media in medias)
                await AddToQueue(media);
        }

        public async void AddMediasToPlaylist(ICollection<Media> medias)
        {
            medias.ToList().ForEach(x => SelectedPlaylist.Medias.Add(x));
            await SavePlaylists();
        }

        public async void OnHotkeyHandler(Hotkey hotkey)
        {
            if (hotkey.Key == Key.MediaNextTrack)
                mainWindow.dataGridQueue.SelectedIndex++;
            else if (hotkey.Key == Key.MediaPreviousTrack)
                await UserControl.ViewModel.PreviousTrack();
            else if (hotkey.Key == Key.MediaPlayPause)
                await UserControl.ViewModel.PlayPause();
        }

        public async Task ChangePlaylist()
        {
            Queue.Clear();
            OldQueue.Clear();
            await UserControl.ViewModel.Stop(true);
            await AddMediasToQueue(SelectedPlaylist.Medias);
            mainWindow.dataGridPlaylists.SelectedItem = null;
        }

        public async Task RemovePlaylist(Playlist playlist)
        {
            Playlists.Remove(playlist);
            await SavePlaylists();
        }

        public ICollection<Playlist> GetPlaylists()
        {
            BinaryFormatter formatter = new BinaryFormatter();
            MemoryStream stream = new MemoryStream();
            byte[] bytes = Unprotect(File.ReadAllBytes(Settings.PlaylistsFilePath));

            stream.Write(bytes, 0, bytes.Length);
            stream.Position = 0;
            ICollection<Playlist> loadedPlaylists = formatter.Deserialize(stream) as ICollection<Playlist>;
            List<Playlist> playlists = new List<Playlist>();
            loadedPlaylists.ToList().ForEach(x => playlists.Add(new Playlist(x.Name, x.Medias)));
            return playlists;
        }

        public async Task<ICollection<Playlist>> GetPlaylistsAsync()
        {
            BinaryFormatter formatter = new BinaryFormatter();
            MemoryStream stream = new MemoryStream();
            byte[] bytes = Unprotect(await File.ReadAllBytesAsync(Settings.PlaylistsFilePath));

            stream.Write(bytes, 0, bytes.Length);
            stream.Position = 0;
            ICollection<Playlist> loadedPlaylists = formatter.Deserialize(stream) as ICollection<Playlist>;
            List<Playlist> playlists = new List<Playlist>();
            loadedPlaylists.ToList().ForEach(x => playlists.Add(new Playlist(x.Name, x.Medias)));
            return playlists;
        }

        private bool GetMouseTargetRow(Visual target, GetPosition position)
        {
            Rect rect = VisualTreeHelper.GetDescendantBounds(target);
            Point point = position((IInputElement)target);
            return rect.Contains(point);
        }

        private DataGridRow GetRowItem(DataGrid dataGrid, int index)
        {
            if (dataGrid.ItemContainerGenerator.Status != GeneratorStatus.ContainersGenerated)
                return null;
            return dataGrid.ItemContainerGenerator.ContainerFromIndex(index) as DataGridRow;
        }

        public int GetCurrentRowIndex(DataGrid dataGrid, GetPosition position)
        {
            int curIndex = -1;
            for (int i = 0; i < dataGrid.Items.Count; i++)
            {
                DataGridRow row = GetRowItem(dataGrid, i);
                if (GetMouseTargetRow(row, position))
                {
                    curIndex = i;
                    break;
                }
            }

            return curIndex;
        }

        public async Task SavePlaylists()
        {
            BinaryFormatter formatter = new BinaryFormatter();
            MemoryStream stream = new MemoryStream();
            formatter.Serialize(stream, Playlists);
            await File.WriteAllBytesAsync(Settings.PlaylistsFilePath, Protect(stream.ToArray()));
        }

        public void OpenSettings()
        {
            mainWindow.flyoutCredits.IsOpen = false;
            mainWindow.flyoutSettings.Position = Position.Left;
            mainWindow.flyoutSettings.IsOpen = !mainWindow.flyoutSettings.IsOpen;
            SettingsOpenedWithEdgeDetection = false;
        }

        public void OpenQueue()
        {
            mainWindow.flyoutQueue.Position = Position.Right;
            mainWindow.flyoutQueue.IsOpen = true;
            QueueOpenedWithEdgeDetection = false;
        }

        public void OpenPlaylists()
        {
            mainWindow.flyoutPlaylists.Position = Position.Right;
            mainWindow.flyoutPlaylists.IsOpen = true;
            PlaylistsOpenedWithEdgeDetection = false;
        }

        public async Task DeletePlaylist(Playlist selectedPlaylist)
        {
            bool mediasExistsInPlaylist = false;

            foreach (Playlist playlist in Playlists.Where(x => x.Name != selectedPlaylist.Name))
            {
                bool exists = selectedPlaylist.Medias.Any(x => playlist.Medias.Any(y => x.Name == y.Name && x.Duration == y.Duration && x.Source == y.Source));

                if (!mediasExistsInPlaylist)
                    mediasExistsInPlaylist = exists;
            }

            if (!mediasExistsInPlaylist)
                selectedPlaylist.Medias.ToList().ForEach(x => File.Delete(x.Source));

            await RemovePlaylist(selectedPlaylist);
        }

        public async Task DeleteMediaFromPlaylist(Playlist playlist, Media media)
        {
            playlist.Medias.Remove(media);
            await SavePlaylists();

            bool existsInPlaylist = Playlists.Any(x => x.Medias.Any(y => y.Name == media.Name));
            if (!existsInPlaylist)
                File.Delete(media.Source);
        }
    }
}
