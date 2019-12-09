using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;

namespace VideoPlayer.Commands
{
    public class UICommands
    {
        public static RoutedUICommand PlayPauseCmd = new RoutedUICommand("Toggle playing", "PlayPause", typeof(UICommands));
    }
}
