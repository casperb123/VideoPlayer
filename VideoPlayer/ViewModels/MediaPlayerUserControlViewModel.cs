using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using VideoPlayer.Entities;
using VideoPlayer.UserControls;
using MaterialDesignThemes.Wpf;

namespace VideoPlayer.ViewModels
{
    public class MediaPlayerUserControlViewModel
    {
        private readonly MediaPlayerUserControl userControl;
        public bool DarkTheme;

        public TimeSpan position;
        public double OldVolume;
        public bool LoopVideo;

        public bool LoopSpecificTime;
        private double loopStart;
        private double loopEnd;

        public DispatcherTimer ProgressTimer;

        public MediaPlayerUserControlViewModel(MediaPlayerUserControl mediaPlayerUserControl, string filePath)
            : this(mediaPlayerUserControl)
        {
            Open(filePath);
        }

        public MediaPlayerUserControlViewModel(MediaPlayerUserControl mediaPlayerUserControl)
        {
            userControl = mediaPlayerUserControl;

            userControl.Focusable = true;
            userControl.Loaded += (s, e) => Keyboard.Focus(userControl);

            userControl.gridMediaElementBackground.Background = new SolidColorBrush(Color.FromRgb(16, 16, 16));
            userControl.comboBoxPlaybackSpeed.SelectedItem = "Normal";

            ProgressTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            ProgressTimer.Tick += (s, e) =>
            {
                if (userControl.player.IsPlaying)
                {
                    if (LoopSpecificTime && loopEnd > loopStart)
                    {
                        if (userControl.sliderProgress.Value < loopStart)
                        {
                            int pos = Convert.ToInt32(loopStart);
                            userControl.player.Position = new TimeSpan(0, 0, 0, pos, 0);
                        }
                        else if (userControl.sliderProgress.Value >= loopEnd)
                        {
                            int pos = Convert.ToInt32(loopStart);
                            userControl.player.Position = new TimeSpan(0, 0, 0, pos, 0);
                        }
                    }

                    userControl.sliderProgress.Value = userControl.player.Position.TotalSeconds;
                }
            };
        }

        public void ShowTime()
        {
            if (userControl.player.IsOpen)
            {
                TimeSpan currentTime = TimeSpan.FromSeconds(userControl.sliderProgress.Value);
                userControl.textBlockDuration.Text = $"{currentTime.ToString(@"m\:ss")} / {userControl.player.NaturalDuration.Value.ToString(@"m\:ss")}";
            }
        }

        public void DisablePlayPause()
        {
            userControl.buttonPlayPause.IsEnabled = false;
            userControl.iconPlayPause.Kind = PackIconKind.Play;
        }

        public void EnablePlayPause()
        {
            userControl.buttonPlayPause.IsEnabled = true;
        }

        public void DisableStop()
        {
            userControl.buttonStop.IsEnabled = false;
        }

        public void EnableStop()
        {
            userControl.buttonStop.IsEnabled = true;
        }

        public void Open(string filePath)
        {
            if (userControl.player.IsOpen)
            {
                ResetLoop();
                ResetProgress();
            }

            Media video = new Media(filePath);
            userControl.player.Source = video.Uri;
            Play();
        }

        public void Play()
        {
            ProgressTimer.Start();
            userControl.player.Play();
            userControl.iconPlayPause.Kind = PackIconKind.Pause;
        }

        public void Pause()
        {
            ProgressTimer.Stop();
            userControl.player.Pause();
            userControl.iconPlayPause.Kind = PackIconKind.Play;
        }

        public void Stop(bool resetSource = true)
        {
            userControl.player.Stop();
            if (resetSource)
            {
                userControl.player.Source = null;
                ResetControls();
            }

            DisablePlayPause();
            DisableStop();

            ProgressTimer.Stop();
            ResetProgress();
        }

        public void ResetControls()
        {
            userControl.textBoxLoopStart.IsEnabled = false;
            userControl.textBoxLoopEnd.IsEnabled = false;
            ResetLoop();
            userControl.checkBoxLoopTime.IsEnabled = false;
            userControl.checkBoxLoopTime.IsChecked = false;
            userControl.sliderProgress.IsEnabled = false;
        }

        public void ResetProgress()
        {
            userControl.sliderProgress.Value = 0;
            userControl.textBlockDuration.Text = "0:00 / 0:00";
        }

        public bool MuteToggle()
        {
            if (userControl.player.Volume > 0)
            {
                OldVolume = userControl.player.Volume;
                userControl.sliderVolume.Value = 0;

                return true;
            }
            else if (OldVolume == 0)
            {
                userControl.sliderVolume.Value = 1;
                OldVolume = userControl.player.Volume;
            }
            else
            {
                userControl.sliderVolume.Value = OldVolume;
                OldVolume = userControl.player.Volume;
            }

            return false;
        }

        public void ToggleSettings()
        {
            userControl.gridSettings.IsEnabled = !userControl.gridSettings.IsEnabled;

            if (userControl.gridSettings.IsEnabled)
            {
                userControl.gridSettings.Visibility = Visibility.Visible;
                userControl.gridSettings.Focus();
            }
            else
            {
                userControl.gridSettings.Visibility = Visibility.Hidden;
            }
        }

        public void ChangeSpeed(double speed)
        {
            userControl.player.SpeedRatio = speed;
        }

        private double ConvertTimeToSeconds(string time)
        {
            double totalSeconds = 0;
            int colonCount = time.Count(x => x == ':');

            if (colonCount == 1)
            {
                int index = time.IndexOf(':');

                double minutes = double.Parse(Split(time, 0, index));
                double seconds = double.Parse(time.Substring(index + 1));

                totalSeconds = (60 * minutes) + seconds;
            }
            else if (colonCount == 2)
            {
                int firstColonIndex = time.IndexOf(':');
                int secondColonIndex = time.Substring(firstColonIndex + 1).IndexOf(':');

                double hours = double.Parse(Split(time, 0, firstColonIndex));
                double minutes = double.Parse(Split(time, firstColonIndex + 1, secondColonIndex));
                double seconds = double.Parse(time.Substring(secondColonIndex + 1));

                totalSeconds = (3600 * hours) + (60 * minutes) + seconds;
            }

            return totalSeconds;
        }

        private string ConvertSecondsToTime(double seconds)
        {
            TimeSpan timeSpan = TimeSpan.FromSeconds(seconds);
            string timeSpanString = string.Empty;

            if (timeSpan.Hours > 0)
            {
                if (timeSpan.Hours > 9)
                {
                    timeSpanString = @"hh\:";
                }
                else
                {
                    timeSpanString = @"h\:";
                }
            }
            if (timeSpan.Minutes > 9 || timeSpan.Hours > 0)
            {
                timeSpanString += @"mm\:";
            }
            else
            {
                timeSpanString += @"m\:";
            }

            return timeSpan.ToString($@"{timeSpanString}ss");
        }

        public void SetLoopTime(string start, string end)
        {
            Regex firstSyntax = new Regex("[0-9]:[0-59]");
            Regex secondSyntax = new Regex("[0-59]:[0-59]");
            Regex thirdSyntax = new Regex("[0-9]:[0-59]:[0-59]");

            if (userControl.player.IsOpen &&
                firstSyntax.IsMatch(start) && firstSyntax.IsMatch(end) ||
                secondSyntax.IsMatch(start) && secondSyntax.IsMatch(end) ||
                thirdSyntax.IsMatch(start) && thirdSyntax.IsMatch(end))
            {
                double startSeconds = ConvertTimeToSeconds(start);
                double endSeconds = ConvertTimeToSeconds(end);

                loopStart = startSeconds;
                loopEnd = endSeconds;

                if (loopStart > 0 && loopEnd > 0 &&
                    loopStart < loopEnd)
                {
                    SetSelection(loopStart, loopEnd);
                    userControl.hyperLinkResetLoop.IsEnabled = true;
                }
                else
                {
                    userControl.hyperLinkResetLoop.IsEnabled = false;
                }
            }
            else
            {
                loopEnd = 0;
                loopStart = 0;
                userControl.hyperLinkResetLoop.IsEnabled = false;
            }
        }

        private string Split(string source, int start, int end)
        {
            if (end < 0)
            {
                end = source.Length + end;
            }
            int len = end - start;

            return source.Substring(start, len);
        }

        public double PixelsToValue(double pixels, double minValue, double maxValue, double width)
        {
            double range = maxValue - minValue;
            double percentage = (pixels / width) * 100;
            return (percentage / 100 * range) + minValue;
        }

        public void SetLoopValue(Point point)
        {
            double value = PixelsToValue(point.X, userControl.sliderProgress.Minimum, userControl.sliderProgress.Maximum, userControl.sliderProgress.ActualWidth);

            if (loopStart == 0 && loopEnd == 0 ||
                Math.Abs(loopStart - value) < Math.Abs(loopEnd - value) && loopEnd > 0)
            {
                userControl.textBoxLoopStart.Text = ConvertSecondsToTime(value);
            }
            else if (loopEnd == 0)
            {
                userControl.textBoxLoopEnd.Text = ConvertSecondsToTime(value);
                userControl.checkBoxLoopTime.IsChecked = true;
            }
            else
            {
                userControl.textBoxLoopEnd.Text = ConvertSecondsToTime(value);
            }
        }

        public void SetSelection(double start, double end)
        {
            userControl.sliderProgress.SelectionStart = start;
            userControl.sliderProgress.SelectionEnd = end;
        }

        public void ResetLoop()
        {
            userControl.textBoxLoopStart.Text = "0:00";
            userControl.textBoxLoopEnd.Text = "0:00";
            userControl.checkBoxLoopTime.IsChecked = false;
        }
    }
}
