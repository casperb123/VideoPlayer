using System;
using System.Windows;
using System.Windows.Input;
using VideoPlayer.UserControls;

namespace VideoPlayer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            masterUserControl.Content = new MediaPlayerUserControl();
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            MediaPlayerUserControl mediaPlayerUserControl = masterUserControl.Content as MediaPlayerUserControl;

            if (e.Key == Key.Space)
            {
                mediaPlayerUserControl.IsPlaying = !mediaPlayerUserControl.IsPlaying;
            }
            else if (e.Key == Key.Right)
            {
                int pos = Convert.ToInt32(mediaPlayerUserControl.sliderProgress.Value + 5);
                mediaPlayerUserControl.player.Position = new TimeSpan(0, 0, 0, pos, 0);
                mediaPlayerUserControl.sliderProgress.Value = mediaPlayerUserControl.player.Position.TotalSeconds;
            }
            else if (e.Key == Key.Left)
            {
                int pos = Convert.ToInt32(mediaPlayerUserControl.sliderProgress.Value - 5);
                mediaPlayerUserControl.player.Position = new TimeSpan(0, 0, 0, pos, 0);
                mediaPlayerUserControl.sliderProgress.Value = mediaPlayerUserControl.player.Position.TotalSeconds;
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            MediaPlayerUserControl mediaPlayerUserControl = masterUserControl.Content as MediaPlayerUserControl;

            if (e.Key == Key.Space)
            {
                mediaPlayerUserControl.IsPlaying = !mediaPlayerUserControl.IsPlaying;
            }
            else if (e.Key == Key.Right)
            {
                int pos = Convert.ToInt32(mediaPlayerUserControl.sliderProgress.Value + 5);
                mediaPlayerUserControl.player.Position = new TimeSpan(0, 0, 0, pos, 0);
                mediaPlayerUserControl.sliderProgress.Value = mediaPlayerUserControl.player.Position.TotalSeconds;
            }
            else if (e.Key == Key.Left)
            {
                int pos = Convert.ToInt32(mediaPlayerUserControl.sliderProgress.Value - 5);
                mediaPlayerUserControl.player.Position = new TimeSpan(0, 0, 0, pos, 0);
                mediaPlayerUserControl.sliderProgress.Value = mediaPlayerUserControl.player.Position.TotalSeconds;
            }
        }
    }
}
