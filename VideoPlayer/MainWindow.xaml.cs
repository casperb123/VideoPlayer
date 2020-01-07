using MahApps.Metro;
using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
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

            string[] cmdLine = Environment.GetCommandLineArgs();
            if (cmdLine.Length >= 2)
            {
                List<string> filePaths = cmdLine.Where(x => validExtensions.Contains(Path.GetExtension(x))).ToList();
                List<Media> medias = new List<Media>();
                filePaths.ForEach(x => medias.Add(new Media(x)));
                ViewModel.AddMediasToQueue(medias).ConfigureAwait(false);
            }

            ViewModel.UserControl = new MediaPlayerUserControl(this);
            masterUserControl.Content = ViewModel.UserControl;
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

        private void CheckBoxLoopTime_Checked(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;

            ViewModel.UserControl.ViewModel.SetLoopTime(textBoxLoopStart.Text, textBoxLoopEnd.Text);
            ViewModel.UserControl.ViewModel.LoopSpecificTime = true;
        }

        private void CheckBoxLoopTime_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;

            ViewModel.UserControl.ViewModel.LoopSpecificTime = false;
            ViewModel.UserControl.ViewModel.SetSelection(0, 0);
        }

        private void CheckBoxLoop_Checked(object sender, RoutedEventArgs e)
        {
            ViewModel.UserControl.ViewModel.LoopVideo = true;
            Focus();
        }

        private void CheckBoxLoop_Unchecked(object sender, RoutedEventArgs e)
        {
            ViewModel.UserControl.ViewModel.LoopVideo = false;
            Focus();
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
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

        private void MetroWindow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!gridFlyoutQueue.IsMouseOver)
                flyoutQueue.IsOpen = false;
            if (!stackPanelFlyoutSettings.IsMouseOver)
                flyoutSettings.IsOpen = false;
            if (!stackPanelFlyoutCredits.IsMouseOver)
                flyoutCredits.IsOpen = false;
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
    }
}
