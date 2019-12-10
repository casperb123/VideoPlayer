using System.Windows;
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
    }
}
