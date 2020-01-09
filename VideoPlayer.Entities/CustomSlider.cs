using System.Windows.Controls;
using System.Windows.Input;

namespace VideoPlayer.Entities
{
    public class CustomSlider : Slider
    {
        public CustomSlider()
        {
            MouseMove += CustomSlider_MouseMove;
        }

        private void CustomSlider_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed)
                return;

            MouseButtonEventArgs args = new MouseButtonEventArgs(e.MouseDevice, e.Timestamp, MouseButton.Left)
            {
                RoutedEvent = MouseLeftButtonDownEvent
            };
            base.OnPreviewMouseLeftButtonDown(args);
        }
    }
}
