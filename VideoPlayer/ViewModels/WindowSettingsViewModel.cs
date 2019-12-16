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
            GetTheme();
            ChangeTheme(window.comboBoxTheme.SelectedItem.ToString(), window.comboBoxColor.SelectedItem as ColorScheme);
        }

        public void GetTheme()
        {
            string theme = Properties.Settings.Default.Theme;
            ColorScheme color = ThemeManager.ColorSchemes.FirstOrDefault(x => x.Name == Properties.Settings.Default.Color);
            window.comboBoxTheme.SelectedItem = theme;
            window.comboBoxColor.SelectedItem = color;
        }

        public void ChangeTheme(string theme, ColorScheme color)
        {
            ThemeManager.ChangeTheme(Application.Current, theme, color.Name);

            //if (theme == "Light")
            //{
            //    window.labelTheme.Foreground = Brushes.Black;
            //    window.labelColor.Foreground = Brushes.Black;
            //}
            //else
            //{
            //    window.labelTheme.Foreground = Brushes.White;
            //    window.labelColor.Foreground = Brushes.White;
            //}

            Properties.Settings.Default.Theme = theme;
            Properties.Settings.Default.Color = color.Name;
            Properties.Settings.Default.Save();
        }
    }
}
