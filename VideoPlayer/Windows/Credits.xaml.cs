using MahApps.Metro.Controls;
using System.Diagnostics;
using System.Windows.Navigation;

namespace VideoPlayer.Windows
{
    /// <summary>
    /// Interaction logic for Credits.xaml
    /// </summary>
    public partial class Credits : MetroWindow
    {
        public Credits()
        {
            InitializeComponent();
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
