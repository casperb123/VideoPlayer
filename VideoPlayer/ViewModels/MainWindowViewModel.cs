using MahApps.Metro;
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

        public MediaPlayerUserControl UserControl;
        public List<Hotkey> Hotkeys;
        public int PlaylistsRowIndex;
        public int PlaylistRowIndex;
        public int QueueRowIndex;

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

        private void OnPropertyChanged(string prop)
        {
            if (prop != null)
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        public MainWindowViewModel(MainWindow mainWindow)
        {
            this.mainWindow = mainWindow;
            queue = new ObservableCollection<Media>();
            oldQueue = new ObservableCollection<Media>();
            playlists = new ObservableCollection<Playlist>();

            ChangeTheme(mainWindow.comboBoxTheme.SelectedItem.ToString(), mainWindow.comboBoxColor.SelectedItem as ColorScheme).ConfigureAwait(false);

            Hotkey nextTrackHotkey = new Hotkey(Key.MediaNextTrack, KeyModifier.None, OnHotkeyHandler, true);
            Hotkey previousTrackHotkey = new Hotkey(Key.MediaPreviousTrack, KeyModifier.None, OnHotkeyHandler, true);
            Hotkey mediaPlayPauseHotkey = new Hotkey(Key.MediaPlayPause, KeyModifier.None, OnHotkeyHandler, true);
            Hotkey spacePlayPauseHotkey = new Hotkey(Key.Space, KeyModifier.None, OnHotkeyHandler, true);
            Hotkey skipForwardHotkey = new Hotkey(Key.Right, KeyModifier.None, OnHotkeyHandler, true);
            Hotkey skipBackwardsHotkey = new Hotkey(Key.Left, KeyModifier.None, OnHotkeyHandler, true);
            Hotkeys = new List<Hotkey>
            {
                nextTrackHotkey,
                previousTrackHotkey,
                mediaPlayPauseHotkey,
                spacePlayPauseHotkey,
                skipForwardHotkey,
                skipBackwardsHotkey
            };
        }

        public async Task ChangeTheme(string theme, ColorScheme color)
        {
            ThemeManager.ChangeTheme(Application.Current, theme, color.Name);
            await GlobalSettings.Settings.Save();
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
            SelectedPlaylist.UpdateMediaCount();
            await SavePlaylists();
        }

        private async void OnHotkeyHandler(Hotkey hotkey)
        {
            if (hotkey.Key == Key.MediaNextTrack)
                mainWindow.dataGridQueue.SelectedIndex++;
            else if (hotkey.Key == Key.MediaPreviousTrack)
                await UserControl.ViewModel.PreviousTrack();
            else if (hotkey.Key == Key.MediaPlayPause)
                await UserControl.ViewModel.PlayPause();
            else if (hotkey.Key == Key.Space && mainWindow.IsActive)
                await UserControl.ViewModel.PlayPause();
            else if (hotkey.Key == Key.Right && mainWindow.IsActive)
                await UserControl.ViewModel.SkipForward(5);
            else if (hotkey.Key == Key.Left && mainWindow.IsActive)
                await UserControl.ViewModel.SkipBackwards(5);
        }

        public async Task ChangePlaylist()
        {
            Queue.Clear();
            OldQueue.Clear();
            await UserControl.ViewModel.Stop(true);
            await AddMediasToQueue(SelectedPlaylist.Medias);
        }

        public void RemovePlaylist()
        {
            string runningPath = AppDomain.CurrentDomain.BaseDirectory;
            string playlistsPath = $@"{runningPath}\Playlists";
            string file = $@"{playlistsPath}\{SelectedPlaylist.Name}.playlist";

            Playlists.Remove(SelectedPlaylist);
            SelectedPlaylist = null;
            if (File.Exists(file))
                File.Delete(file);
        }

        public async Task<ICollection<Playlist>> GetPlaylists()
        {
            string runningPath = AppDomain.CurrentDomain.BaseDirectory;
            string file = $@"{runningPath}\Playlists.bin";
            BinaryFormatter formatter = new BinaryFormatter();
            MemoryStream stream = new MemoryStream();
            byte[] bytes = Unprotect(await File.ReadAllBytesAsync(file));

            stream.Write(bytes, 0, bytes.Length);
            stream.Position = 0;
            ICollection<Playlist> playlists = formatter.Deserialize(stream) as ICollection<Playlist>;
            return playlists;
        }

        public void RenamePlaylist(Playlist playlist, string name)
        {
            string runningPath = AppDomain.CurrentDomain.BaseDirectory;
            string playlistsPath = $@"{runningPath}\Playlists";
            string file = $@"{playlistsPath}\{playlist.Name}.playlist";
            if (File.Exists(file))
            {
                string newFile = $@"{playlistsPath}\{name}.playlist";
                File.Move(file, newFile);
            }
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
            string runningPath = AppDomain.CurrentDomain.BaseDirectory;
            string file = $@"{runningPath}\Playlists.bin";

            BinaryFormatter formatter = new BinaryFormatter();
            MemoryStream stream = new MemoryStream();
            formatter.Serialize(stream, Playlists);
            await File.WriteAllBytesAsync(file, Protect(stream.ToArray()));
        }
    }
}
