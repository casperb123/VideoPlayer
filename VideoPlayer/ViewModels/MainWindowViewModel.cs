using MahApps.Metro;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace VideoPlayer.ViewModels
{
    public class MainWindowViewModel
    {
        private readonly MainWindow mainWindow;

        public MainWindowViewModel(MainWindow mainWindow)
        {
            this.mainWindow = mainWindow;
            ChangeTheme(mainWindow.comboBoxTheme.SelectedItem.ToString(), mainWindow.comboBoxColor.SelectedItem as ColorScheme);
        }

        public void ChangeTheme(string theme, ColorScheme color)
        {
            ThemeManager.ChangeTheme(Application.Current, theme, color.Name);
            Properties.Settings.Default.Save();
        }
    }
}
