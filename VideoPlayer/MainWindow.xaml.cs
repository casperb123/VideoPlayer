using System;
using System.IO;
using System.Linq;
using System.Windows;
using VideoPlayer.UserControls;

namespace VideoPlayer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
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
            string[] cmdLine = Environment.GetCommandLineArgs();
            if (cmdLine.Length == 2 && validExtensions.Contains(Path.GetExtension(cmdLine[1])))
            {
                masterUserControl.Content = new MediaPlayerUserControl(cmdLine[1]);
            }
            else
            {
                masterUserControl.Content = new MediaPlayerUserControl();
            }
        }
    }
}
