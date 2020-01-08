﻿using MahApps.Metro;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using VideoPlayer.Entities;
using VideoPlayer.UserControls;

namespace VideoPlayer.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged
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
            this.playlists = new ObservableCollection<Playlist>();
            ChangeTheme(mainWindow.comboBoxTheme.SelectedItem.ToString(), mainWindow.comboBoxColor.SelectedItem as ColorScheme);

            Hotkey nextTrackHotkey = new Hotkey(Key.MediaNextTrack, KeyModifier.None, OnHotkeyHandler, true);
            Hotkey previousTrackHotkey = new Hotkey(Key.MediaPreviousTrack, KeyModifier.None, OnHotkeyHandler, true);
            Hotkey playPauseHotkey = new Hotkey(Key.MediaPlayPause, KeyModifier.None, OnHotkeyHandler, true);
            Hotkeys = new List<Hotkey>
            {
                nextTrackHotkey,
                previousTrackHotkey,
                playPauseHotkey
            };
        }

        public void ChangeTheme(string theme, ColorScheme color)
        {
            ThemeManager.ChangeTheme(Application.Current, theme, color.Name);
            Properties.Settings.Default.Save();
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
            await SelectedPlaylist.Save(true);
        }

        private async void OnHotkeyHandler(Hotkey hotkey)
        {
            if (hotkey.Key == Key.MediaNextTrack)
            {
                mainWindow.dataGridQueue.SelectedIndex++;
            }
            else if (hotkey.Key == Key.MediaPreviousTrack)
            {
                await UserControl.ViewModel.PreviousTrack();
            }
            else if (hotkey.Key == Key.MediaPlayPause)
            {
                MediaPlayerUserControl.PlayPauseCmd.Execute(null, UserControl.buttonPlayPause);
            }
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
            string playlistsPath = $@"{runningPath}\Playlists";
            if (!Directory.Exists(playlistsPath))
                Directory.CreateDirectory(playlistsPath);
            string[] files = Directory.GetFiles(playlistsPath, "*.playlist");
            List<Playlist> playlists = new List<Playlist>();
            foreach (string file in files)
            {
                string[] filePaths = await File.ReadAllLinesAsync(file);
                Playlist playlist = UserControl.ViewModel.GetPlaylist(Path.GetFileNameWithoutExtension(file), filePaths);
                playlists.Add(playlist);
            }

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
    }
}