using MahApps.Metro;
using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using VideoPlayer.ViewModels;

namespace VideoPlayer.Windows
{
    /// <summary>
    /// Interaction logic for WindowSettings.xaml
    /// </summary>
    public partial class WindowSettings : MetroWindow
    {
        private readonly WindowSettingsViewModel viewModel;

        public WindowSettings(bool closeAfterOpened = false)
        {
            InitializeComponent();
            viewModel = new WindowSettingsViewModel(this);
            DataContext = viewModel;

            if (closeAfterOpened)
                Close();
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;

            viewModel.ChangeTheme(comboBoxTheme.SelectedItem.ToString(), comboBoxColor.SelectedItem as ColorScheme);
        }
    }
}
