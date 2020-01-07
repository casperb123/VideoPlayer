using MahApps.Metro;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
                    throw new ArgumentNullException("The old queue can't be null");

                oldQueue = value;
            }
        }

        private void OnPropertyChanged(string prop)
        {
            if (prop != null)
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }

        public MainWindowViewModel(MainWindow mainWindow)
        {
            this.mainWindow = mainWindow;
            queue = new ObservableCollection<Media>();
            oldQueue = new ObservableCollection<Media>();
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

        public async Task AddMediasToQueue(List<Media> medias)
        {
            foreach (Media media in medias)
            {
                await AddToQueue(media);
            }
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
    }
}
