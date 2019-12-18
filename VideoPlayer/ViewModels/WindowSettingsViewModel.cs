using MahApps.Metro;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using VideoPlayer.Windows;

namespace VideoPlayer.ViewModels
{
    public class WindowSettingsViewModel
    {
        private readonly WindowSettings window;

        public WindowSettingsViewModel(WindowSettings windowSettings)
        {
            window = windowSettings;
            ChangeTheme(window.comboBoxTheme.SelectedItem.ToString(), window.comboBoxColor.SelectedItem as ColorScheme);
        }

        public void ChangeTheme(string theme, ColorScheme color)
        {
            ThemeManager.ChangeTheme(Application.Current, theme, color.Name);
            Properties.Settings.Default.Save();
        }
    }
}
