using ControlzEx.Theming;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
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
            IEnumerable<Settings.EdgeOpen> edgeOpens = Enum.GetValues(typeof(Settings.EdgeOpen)).Cast<Settings.EdgeOpen>();
            comboBoxRightEdgeOpen.ItemsSource = edgeOpens;
            comboBoxLeftEdgeOpen.ItemsSource = edgeOpens;

            comboBoxTheme.ItemsSource = ThemeManager.Current.BaseColors;
            comboBoxColor.ItemsSource = ThemeManager.Current.ColorSchemes;

            Binding rightEdgeBinding = new Binding("RightEdgeOpen")
            {
                Source = Settings.CurrentSettings
            };
            comboBoxRightEdgeOpen.SetBinding(Selector.SelectedItemProperty, rightEdgeBinding);
            Binding leftEdgeBinding = new Binding("LeftEdgeOpen")
            {
                Source = Settings.CurrentSettings
            };
            comboBoxLeftEdgeOpen.SetBinding(Selector.SelectedItemProperty, leftEdgeBinding);

            ViewModel.ChangeTheme(comboBoxTheme.SelectedItem.ToString(), comboBoxColor.SelectedItem.ToString()).ConfigureAwait(false);

            if (File.Exists(Settings.PlaylistsFilePath))
            {
                ICollection<Playlist> playlists = ViewModel.GetPlaylists();
                playlists.ToList().ForEach(x => ViewModel.Playlists.Add(x));
            }

            string[] files = Directory.GetFiles(Settings.MediasPath);

            foreach (string file in files)
            {
                bool existsInPlaylist = ViewModel.Playlists.Any(x => x.Medias.Any(y => y.Source == file));

                if (!existsInPlaylist)
                    File.Delete(file);
            }

            string[] cmdLine = Environment.GetCommandLineArgs();
            List<string> filePaths = cmdLine.Where(x => validExtensions.Contains(Path.GetExtension(x))).ToList();
            if (filePaths.Count > 0)
            {
                List<Media> medias = new List<Media>();
                filePaths.ForEach(x => medias.Add(new Media(x)));
                ViewModel.AddMediasToQueue(medias).ConfigureAwait(false);
            }

            RegisterCommandBindings();
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

        private void Settings_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            ViewModel.OpenSettings();
        }

        private void Queue_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            ViewModel.OpenQueue();
        }

        private void Playlists_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            ViewModel.OpenPlaylists();
        }

        private void ExitFullscreen_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (ViewModel.UserControl.ViewModel.IsFullscreen)
                ViewModel.UserControl.ViewModel.ExitFullscreen();
        }

        private void RegisterCommandBindings()
        {
            RoutedCommand PlayPauseCommand = new RoutedCommand();
            RoutedCommand SkipForwardCommand = new RoutedCommand();
            RoutedCommand SkipBackwardsCommand = new RoutedCommand();
            RoutedCommand SettingsCommand = new RoutedCommand();
            RoutedCommand QueueCommand = new RoutedCommand();
            RoutedCommand PlaylistsCommand = new RoutedCommand();
            RoutedCommand ExitFullscreenCommand = new RoutedCommand();

            PlayPauseCommand.InputGestures.Add(new KeyGesture(Key.Space));
            SkipForwardCommand.InputGestures.Add(new KeyGesture(Key.Right));
            SkipBackwardsCommand.InputGestures.Add(new KeyGesture(Key.Left));
            SettingsCommand.InputGestures.Add(new KeyGesture(Key.S, ModifierKeys.Control));
            QueueCommand.InputGestures.Add(new KeyGesture(Key.Q, ModifierKeys.Control));
            PlaylistsCommand.InputGestures.Add(new KeyGesture(Key.P, ModifierKeys.Control));
            ExitFullscreenCommand.InputGestures.Add(new KeyGesture(Key.Escape));

            CommandBinding PlayPauseBinding = new CommandBinding(PlayPauseCommand, PlayPause_Executed);
            CommandBinding SkipForwardBinding = new CommandBinding(SkipForwardCommand, SkipForward_Executed);
            CommandBinding SkipBackwardsBinding = new CommandBinding(SkipBackwardsCommand, SkipBackwards_Executed);
            CommandBinding SettingsBinding = new CommandBinding(SettingsCommand, Settings_Executed);
            CommandBinding QueueBinding = new CommandBinding(QueueCommand, Queue_Executed);
            CommandBinding PlaylistsBinding = new CommandBinding(PlaylistsCommand, Playlists_Executed);
            CommandBinding ExitFullscreenBinding = new CommandBinding(ExitFullscreenCommand, ExitFullscreen_Executed);

            CommandBinding[] commandBindings = new CommandBinding[]
            {
                PlayPauseBinding,
                SkipForwardBinding,
                SkipBackwardsBinding,
                SettingsBinding,
                QueueBinding,
                PlaylistsBinding,
                ExitFullscreenBinding
            };
            CommandBindings.AddRange(commandBindings);
        }

        private void ButtonWindowSettings_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.OpenSettings();
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
            await ViewModel.SavePlaylists();
        }

        private void NumericPlaybackSpeed_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            if (!IsLoaded || !e.NewValue.HasValue)
                return;

            ViewModel.UserControl.ViewModel.ChangeSpeed(e.NewValue.Value);
        }

        private void NumericPitch_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            if (!IsLoaded || !e.NewValue.HasValue)
                return;

            ViewModel.UserControl.ViewModel.ChangePitch((int)e.NewValue.Value);
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
            string color = comboBoxColor.SelectedItem.ToString();
            ThemeManager.Current.ChangeThemeBaseColor(Application.Current, theme);
            ThemeManager.Current.ChangeThemeColorScheme(Application.Current, color);
            ViewModel.SettingsChanged = true;
        }

        private async void DataGridQueue_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!(dataGridQueue.SelectedItem is Media selectedMedia))
                return;

            await ViewModel.UserControl.ViewModel.ChangeTrack(selectedMedia);
        }

        private void ButtonClearQueue_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.Queue.Clear();
            ViewModel.UserControl.buttonSkipForward.IsEnabled = false;
        }

        private void MenuItemQueueRemove_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.Queue.Remove(ViewModel.SelectedMedia);
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
        }

        private void ToggleSwitchLoop_Unchecked(object sender, RoutedEventArgs e)
        {
            ViewModel.UserControl.ViewModel.LoopVideo = false;
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

        private void ContextMenuDataGridPlaylists_Opened(object sender, RoutedEventArgs e)
        {
            ViewModel.IsAnyContextOpen = true;
            if (ViewModel.SelectedPlaylist is null)
            {
                menuItemPlaylistsRemove.IsEnabled = false;
                menuItemPlaylistsEditName.IsEnabled = false;
                menuItemPlaylistsEditMedias.IsEnabled = false;
                menuItemPlaylistsAddToQueue.IsEnabled = false;
            }
            else
            {
                menuItemPlaylistsRemove.IsEnabled = true;
                menuItemPlaylistsEditName.IsEnabled = true;
                menuItemPlaylistsEditMedias.IsEnabled = true;
                menuItemPlaylistsAddToQueue.IsEnabled = true;
            }
        }

        private void ContextMenuDataGridPlaylists_Closed(object sender, RoutedEventArgs e)
        {
            ViewModel.IsAnyContextOpen = false;
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
                foreach (string file in fileNames)
                {
                    string destination = $@"{Settings.MediasPath}\{Path.GetFileName(file)}";
                    if (!File.Exists(destination))
                        File.Copy(file, destination);
                }
                fileNames.ToList().ForEach(x => fileNames[fileNames.IndexOf(x)] = $@"{Settings.MediasPath}\{Path.GetFileName(x)}");

                List<Media> medias = new List<Media>();
                fileNames.ForEach(x => medias.Add(new Media(x)));
                ViewModel.AddMediasToPlaylist(medias);
            }
        }

        private async void MenuItemPlaylistsRemove_Click(object sender, RoutedEventArgs e)
        {
            MessageDialogResult result = await this.ShowMessageAsync("Delete playlist", $"Are you sure that you want to delete the playlist '{ViewModel.SelectedPlaylist.Name}'", MessageDialogStyle.AffirmativeAndNegative);
            if (result == MessageDialogResult.Affirmative)
            {
                await ViewModel.DeletePlaylist(ViewModel.SelectedPlaylist);
            }
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

                ViewModel.SelectedPlaylist = playlist;
                flyoutPlaylist.IsOpen = true;
            }
        }

        private async void FlyoutPlaylist_IsOpenChanged(object sender, RoutedEventArgs e)
        {
            flyoutPlaylists.IsPinned = flyoutPlaylist.IsOpen;
            if (!flyoutPlaylist.IsOpen)
            {
                if (ViewModel.SelectedPlaylist != null && ViewModel.SelectedPlaylist.MediasChanged)
                {
                    await ViewModel.SavePlaylists();
                    ViewModel.SelectedPlaylist.MediasChanged = false;
                }

                dataGridPlaylist.SelectedItem = null;
            }
        }

        private void ContextMenuDataGridPlaylist_Opened(object sender, RoutedEventArgs e)
        {
            ViewModel.IsAnyContextOpen = true;
            if (ViewModel.SelectedPlaylistMedia is null)
                menuItemPlaylistRemove.IsEnabled = false;
            else
                menuItemPlaylistRemove.IsEnabled = true;
        }

        private void ContextMenuDataGridPlaylist_Closed(object sender, RoutedEventArgs e)
        {
            ViewModel.IsAnyContextOpen = false;
        }

        private async void MenuItemPlaylistRemove_Click(object sender, RoutedEventArgs e)
        {
            MessageDialogResult result = await this.ShowMessageAsync("Remove media", $"Are you sure that you want to remove the media '{ViewModel.SelectedPlaylistMedia.Name}' from the playlist '{ViewModel.SelectedPlaylist.Name}'?", MessageDialogStyle.AffirmativeAndNegative);
            if (result == MessageDialogResult.Affirmative)
            {
                await ViewModel.DeleteMediaFromPlaylist(ViewModel.SelectedPlaylist, ViewModel.SelectedPlaylistMedia);
            }
        }

        private async void MenuItemPlaylistsAddToQueue_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.AddMediasToQueue(ViewModel.SelectedPlaylist.Medias);
        }

        private void DataGridPlaylists_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ViewModel.PlaylistsRowIndex = ViewModel.GetCurrentRowIndex(dataGridPlaylists, e.GetPosition);
            if (ViewModel.PlaylistsRowIndex == -1)
                return;
            if (!(dataGridPlaylists.Items[ViewModel.PlaylistsRowIndex] is Playlist selectedPlaylist))
                return;

            ViewModel.PlaylistsPlaylistSelected = true;
            DragDrop.DoDragDrop(dataGridPlaylists, selectedPlaylist, DragDropEffects.Move);
            if (ViewModel.PlaylistsPlaylistSelected)
                dataGridPlaylists.SelectedIndex = ViewModel.PlaylistsRowIndex;
        }

        private void DataGridPlaylists_Drop(object sender, DragEventArgs e)
        {
            if (ViewModel.PlaylistRowIndex < 0)
                return;
            int index = ViewModel.GetCurrentRowIndex(dataGridPlaylists, e.GetPosition);
            if (index < 0 || index == ViewModel.PlaylistsRowIndex)
                return;

            Playlist changedPlaylist = ViewModel.Playlists[ViewModel.PlaylistsRowIndex];
            ViewModel.Playlists.RemoveAt(ViewModel.PlaylistsRowIndex);
            ViewModel.Playlists.Insert(index, changedPlaylist);
            ViewModel.PlaylistsChanged = true;
            ViewModel.PlaylistsPlaylistSelected = false;
        }

        private void DataGridPlaylist_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ViewModel.PlaylistRowIndex = ViewModel.GetCurrentRowIndex(dataGridPlaylist, e.GetPosition);
            if (ViewModel.PlaylistRowIndex == -1)
                return;
            if (!(dataGridPlaylist.Items[ViewModel.PlaylistRowIndex] is Media selectedMedia))
                return;
            ViewModel.PlaylistMediaSelected = true;
            DragDrop.DoDragDrop(dataGridPlaylist, selectedMedia, DragDropEffects.Move);
            if (ViewModel.PlaylistMediaSelected)
                dataGridPlaylist.SelectedIndex = ViewModel.PlaylistRowIndex;
        }

        private void DataGridPlaylist_Drop(object sender, DragEventArgs e)
        {
            if (ViewModel.PlaylistRowIndex < 0)
                return;
            int index = ViewModel.GetCurrentRowIndex(dataGridPlaylist, e.GetPosition);
            if (index < 0 || index == ViewModel.PlaylistRowIndex)
                return;

            Media changedMedia = ViewModel.SelectedPlaylist.Medias[ViewModel.PlaylistRowIndex];
            ViewModel.SelectedPlaylist.Medias.RemoveAt(ViewModel.PlaylistRowIndex);
            ViewModel.SelectedPlaylist.Medias.Insert(index, changedMedia);
            ViewModel.SelectedPlaylist.MediasChanged = true;
            ViewModel.PlaylistMediaSelected = false;
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
            if (index == -1 || !(dataGridPlaylists.Items[index] is Playlist selectedPlaylist))
                return;
            ViewModel.SelectedPlaylist = selectedPlaylist;
            contextMenuDataGridPlaylists.IsOpen = true;
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
            try
            {
                bool update = await ViewModel.Updater.CheckForUpdateAsync();

                if (!update)
                    await this.ShowMessageAsync("Up to date", "You're already using the latest version of the application");
            }
            catch (WebException ex)
            {
                if (ex.InnerException is null)
                    await this.ShowMessageAsync("Checking for updates failed", ex.Message);
                else
                    await this.ShowMessageAsync("Checking for updates failed", ex.InnerException.Message);
            }
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
            if (!ViewModel.UserControl.ViewModel.IsFullscreen ||
                !(sender is Flyout flyout))
                return;
            if (Settings.CurrentSettings.LeftRightEdgeDetection)
                ViewModel.UserControl.ViewModel.ControlsTimer.Start();

            string settingsName = flyout.Name.Replace("flyout", "");

            if (settingsName == "Queue" && !ViewModel.QueueOpenedWithEdgeDetection ||
                settingsName == "Playlists" && !ViewModel.PlaylistsOpenedWithEdgeDetection ||
                settingsName == "Settings" && !ViewModel.SettingsOpenedWithEdgeDetection)
                return;

            if (Settings.CurrentSettings.LeftEdgeOpen.ToString() == settingsName ||
                Settings.CurrentSettings.RightEdgeOpen.ToString() == settingsName)
                flyout.IsOpen = false;
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

        private void ComboBoxRightEdgeOpen_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded)
                return;

            ViewModel.SettingsChanged = true;
        }

        private void ToggleSwitchLeftRightEdgeDetection_Toggled(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded)
                return;

            ViewModel.SettingsChanged = true;
        }

        private void NumericUpDownLeftRightEdgeDistance_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            if (!IsLoaded)
                return;

            ViewModel.SettingsChanged = true;
        }

        private void MetroWindow_MouseMove(object sender, MouseEventArgs e)
        {

            if (!ViewModel.UserControl.ViewModel.IsFullscreen || ViewModel.UserControl.ViewModel.MouseOverControls)
                return;

            Point mousePos = Mouse.GetPosition(this);
            double xRight = ActualWidth - mousePos.X;

            if (Settings.CurrentSettings.TopEdgeDetection &&
                !ViewModel.IsFlyoutOpen)
            {
                if (mousePos.Y <= 5 && !ShowTitleBar)
                {
                    ShowCloseButton = true;
                    ShowTitleBar = true;
                }
                else if (mousePos.Y > 30 && ShowTitleBar)
                {
                    ShowCloseButton = false;
                    ShowTitleBar = false;
                }
            }

            if (Settings.CurrentSettings.LeftRightEdgeDetection && !ShowTitleBar && !flyoutCredits.IsOpen)
            {
                if (xRight <= Settings.CurrentSettings.LeftRightEdgeDistance)
                {
                    if (flyoutQueue.IsOpen && flyoutQueue.Position == Position.Right ||
                        flyoutPlaylists.IsOpen && flyoutPlaylists.Position == Position.Right ||
                        flyoutSettings.IsOpen && flyoutSettings.Position == Position.Right)
                        return;

                    if (Settings.CurrentSettings.RightEdgeOpen == Settings.EdgeOpen.Queue && !flyoutQueue.IsOpen)
                    {
                        flyoutQueue.Position = Position.Right;
                        flyoutQueue.IsOpen = true;
                        ViewModel.QueueOpenedWithEdgeDetection = true;
                    }
                    else if (Settings.CurrentSettings.RightEdgeOpen == Settings.EdgeOpen.Playlists && !flyoutPlaylists.IsOpen)
                    {
                        flyoutPlaylist.Position = Position.Right;
                        flyoutPlaylists.IsOpen = true;
                        ViewModel.PlaylistsOpenedWithEdgeDetection = true;
                    }
                    else if (Settings.CurrentSettings.RightEdgeOpen == Settings.EdgeOpen.Settings && !flyoutSettings.IsOpen)
                    {
                        flyoutSettings.Position = Position.Right;
                        flyoutSettings.IsOpen = true;
                        ViewModel.SettingsOpenedWithEdgeDetection = true;
                    }
                }
                else if (mousePos.X <= Settings.CurrentSettings.LeftRightEdgeDistance)
                {
                    if (flyoutQueue.IsOpen && flyoutQueue.Position == Position.Left ||
                        flyoutPlaylists.IsOpen && flyoutPlaylists.Position == Position.Left ||
                        flyoutSettings.IsOpen && flyoutSettings.Position == Position.Left)
                        return;

                    if (Settings.CurrentSettings.LeftEdgeOpen == Settings.EdgeOpen.Queue && !flyoutQueue.IsOpen)
                    {
                        flyoutQueue.Position = Position.Left;
                        flyoutQueue.IsOpen = true;
                        ViewModel.QueueOpenedWithEdgeDetection = true;
                    }
                    else if (Settings.CurrentSettings.LeftEdgeOpen == Settings.EdgeOpen.Playlists && !flyoutPlaylists.IsOpen)
                    {
                        flyoutPlaylist.Position = Position.Left;
                        flyoutPlaylists.IsOpen = true;
                        ViewModel.PlaylistsOpenedWithEdgeDetection = true;
                    }
                    else if (Settings.CurrentSettings.LeftEdgeOpen == Settings.EdgeOpen.Settings && !flyoutSettings.IsOpen)
                    {
                        flyoutSettings.Position = Position.Left;
                        flyoutSettings.IsOpen = true;
                        ViewModel.SettingsOpenedWithEdgeDetection = true;
                    }
                }
            }
        }

        private void FlyoutQueue_IsOpenChanged(object sender, RoutedEventArgs e)
        {
            if (flyoutQueue.IsOpen)
            {
                flyoutPlaylists.IsOpen = false;
                flyoutPlaylist.IsOpen = false;
            }
        }

        private async void FlyoutPlaylists_IsOpenChanged(object sender, RoutedEventArgs e)
        {
            if (flyoutPlaylists.IsOpen)
                flyoutQueue.IsOpen = false;
            else
            {
                if (ViewModel.PlaylistsChanged)
                {
                    await ViewModel.SavePlaylists();
                    ViewModel.PlaylistsChanged = false;
                }
            }
        }

        private void DataGridQueue_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            int index = ViewModel.GetCurrentRowIndex(dataGridQueue, e.GetPosition);
            if (index == -1 || !(dataGridQueue.Items[index] is Media selectedMedia))
                return;
            ViewModel.SelectedMedia = selectedMedia;
            contextMenuDataGridQueue.IsOpen = true;
        }

        private void ContextMenuDataGridQueue_Opened(object sender, RoutedEventArgs e)
        {
            ViewModel.IsAnyContextOpen = true;
            if (ViewModel.SelectedMedia is null)
                menuItemQueueRemove.IsEnabled = false;
            else
                menuItemQueueRemove.IsEnabled = true;
        }

        private void ContextMenuDataGridQueue_Closed(object sender, RoutedEventArgs e)
        {
            ViewModel.IsAnyContextOpen = false;
        }

        private void DataGridPlaylist_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            int index = ViewModel.GetCurrentRowIndex(dataGridPlaylist, e.GetPosition);
            if (index == -1 || !(dataGridPlaylist.Items[index] is Media selectedMedia))
                return;
            ViewModel.SelectedPlaylistMedia = selectedMedia;
            contextMenuDataGridPlaylist.IsOpen = true;
        }

        private void DataGridQueue_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        private void DataGridPlaylists_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        private void DataGridPlaylist_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        private void ToggleSwitchAlwaysOnTop_IsCheckedChanged(object sender, EventArgs e)
        {
            if (!IsLoaded)
                return;

            ViewModel.SettingsChanged = true;
        }

        private void MetroWindow_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point point = e.GetPosition(this);

            if (ViewModel.UserControl.ViewModel.IsFullscreen && e.Source == this && point.Y <= 35)
                e.Handled = true;
        }

        private void ToggleSwitchTopEdgeDetection_Toggled(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded)
                return;

            ViewModel.SettingsChanged = true;
        }

        private void FlyoutCredits_IsOpenChanged(object sender, RoutedEventArgs e)
        {
            if (flyoutCredits.IsOpen && ViewModel.UserControl.ViewModel.IsFullscreen)
            {
                ShowCloseButton = false;
                ShowTitleBar = false;
            }
        }

        private void ToggleSwitchLoopTime_Toggled(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;

            if (toggleSwitchLoopTime.IsOn)
            {
                ViewModel.UserControl.ViewModel.SetLoopTime(textBoxLoopStart.Text, textBoxLoopEnd.Text);
                ViewModel.UserControl.ViewModel.LoopSpecificTime = true;
            }
            else
            {
                ViewModel.UserControl.ViewModel.LoopSpecificTime = false;
                ViewModel.UserControl.ViewModel.SetSelection(0, 0);
            }
        }

        private void ToggleSwitchLoop_Toggled(object sender, RoutedEventArgs e)
        {
            ViewModel.UserControl.ViewModel.LoopVideo = toggleSwitchLoop.IsOn;
        }

        private void ToggleSwitchAlwaysOnTop_Toggled(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded)
                return;

            ViewModel.SettingsChanged = true;
        }

        private void ToggleSwitchCheckForUpdates_Toggled(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded)
                return;

            ViewModel.SettingsChanged = true;
        }
    }
}
