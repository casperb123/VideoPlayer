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
using VideoPlayer.Entities;
using VideoPlayer.UserControls;
using VideoPlayer.ViewModels;
using VideoPlayer.Windows;

namespace VideoPlayer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        private MediaPlayerUserControl mediaPlayerUserControl;
        private MainWindowViewModel viewModel;

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
            viewModel = new MainWindowViewModel(this);
            DataContext = viewModel;

            LeftWindowCommandsOverlayBehavior = WindowCommandsOverlayBehavior.HiddenTitleBar;
            IconOverlayBehavior = OverlayBehavior.Flyouts;

            //WindowSettings windowSettings = new WindowSettings(true);

            string[] cmdLine = Environment.GetCommandLineArgs();
            if (cmdLine.Length >= 2)
            {
                List<string> filePaths = cmdLine.Where(x => validExtensions.Contains(Path.GetExtension(x))).ToList();
                List<Media> medias = new List<Media>();
                filePaths.ForEach(x => medias.Add(new Media(x)));
                mediaPlayerUserControl = new MediaPlayerUserControl(medias);
                masterUserControl.Content = mediaPlayerUserControl;
            }
            else
            {
                mediaPlayerUserControl = new MediaPlayerUserControl();
                masterUserControl.Content = mediaPlayerUserControl;
            }
        }

        private void ButtonWindowSettings_Click(object sender, RoutedEventArgs e)
        {
            flyOutSettings.IsOpen = !flyOutSettings.IsOpen;
            //WindowSettings windowSettings = new WindowSettings();
            //windowSettings.Owner = this;
            //windowSettings.ShowDialog();
        }

        private void ButtonWindowCredits_Click(object sender, RoutedEventArgs e)
        {
            Credits credits = new Credits();
            credits.Owner = this;
            credits.ShowDialog();
        }

        private void MetroWindow_Closing(object sender, CancelEventArgs e)
        {
            MediaPlayerUserControl userControl = masterUserControl.Content as MediaPlayerUserControl;
            userControl.ViewModel.Hotkeys.ForEach(x => x.Dispose());
        }

        private void NumericPlaybackSpeed_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            if (!IsLoaded || !e.NewValue.HasValue) return;

            mediaPlayerUserControl.ViewModel.ChangeSpeed(e.NewValue.Value);
        }

        private void TextBoxLoop_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!IsLoaded) return;

            mediaPlayerUserControl.ViewModel.SetLoopTime(textBoxLoopStart.Text, textBoxLoopEnd.Text);
        }

        private void CheckBoxLoopTime_Checked(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;

            mediaPlayerUserControl.ViewModel.SetLoopTime(textBoxLoopStart.Text, textBoxLoopEnd.Text);
            mediaPlayerUserControl.ViewModel.LoopSpecificTime = true;
        }

        private void CheckBoxLoopTime_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;

            mediaPlayerUserControl.ViewModel.LoopSpecificTime = false;
            mediaPlayerUserControl.ViewModel.SetSelection(0, 0);
        }

        private void CheckBoxLoop_Checked(object sender, RoutedEventArgs e)
        {
            mediaPlayerUserControl.ViewModel.LoopVideo = true;
            Focus();
        }

        private void CheckBoxLoop_Unchecked(object sender, RoutedEventArgs e)
        {
            mediaPlayerUserControl.ViewModel.LoopVideo = false;
            Focus();
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;

            viewModel.ChangeTheme(comboBoxTheme.SelectedItem.ToString(), comboBoxColor.SelectedItem as ColorScheme);
        }
    }
}
