using MahApps.Metro;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.ComTypes;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Unosquare.FFME.Common;
using VideoPlayer.Entities;
using VideoPlayer.UserControls;

namespace VideoPlayer.ViewModels
{
    public class MediaPlayerUserControlViewModel
    {
        private readonly MediaPlayerUserControl userControl;
        public bool DarkTheme;

        public readonly Image PlayImage;
        public readonly Image PlayImageWhite;
        public readonly Image PlayImageDisabled;

        public readonly Image PauseImage;
        public readonly Image PauseImageWhite;
        public readonly Image PauseImageDisabled;

        public readonly Image StopImage;
        public readonly Image StopImageWhite;
        public readonly Image StopImageDisabled;

        public readonly Image OpenImage;
        public readonly Image OpenImageWhite;
        public readonly Image OpenImageDisabled;

        public readonly Image MutedImage;
        public readonly Image MutedImageWhite;

        public readonly Image HighVolumeImage;
        public readonly Image HighVolumeImageWhite;

        public readonly Image LowVolumeImage;
        public readonly Image LowVolumeImageWhite;

        public readonly Image SettingsImage;
        public readonly Image SettingsImageWhite;
        public readonly Image SettingsImageDisabled;

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

            string runningPath = AppDomain.CurrentDomain.BaseDirectory;
            string resourcesPath = $@"{Path.GetFullPath(Path.Combine(runningPath, @"..\..\..\"))}Resources";

            if (!Directory.Exists(resourcesPath))
            {
                resourcesPath = $@"{runningPath}\Resources";
            }

            PlayImage = new Image
            {
                Width = userControl.buttonPlayPause.Width,
                Source = new BitmapImage(new Uri($@"{resourcesPath}\play.png"))
            };
            PlayImageWhite = new Image
            {
                Width = userControl.buttonPlayPause.Width,
                Source = new BitmapImage(new Uri($@"{resourcesPath}\play-white.png"))
            };
            PlayImageDisabled = new Image
            {
                Width = userControl.buttonPlayPause.Width,
                Source = new BitmapImage(new Uri($@"{resourcesPath}\play-disabled.png"))
            };

            PauseImage = new Image
            {
                Width = userControl.buttonPlayPause.Width,
                Source = new BitmapImage(new Uri($@"{resourcesPath}\pause.png"))
            };
            PauseImageWhite = new Image
            {
                Width = userControl.buttonPlayPause.Width,
                Source = new BitmapImage(new Uri($@"{resourcesPath}\pause-white.png"))
            };
            PauseImageDisabled = new Image
            {
                Width = userControl.buttonPlayPause.Width,
                Source = new BitmapImage(new Uri($@"{resourcesPath}\pause-disabled.png"))
            };

            StopImage = new Image
            {
                Width = userControl.buttonStop.Width,
                Source = new BitmapImage(new Uri($@"{resourcesPath}\stop.png"))
            };
            StopImageWhite = new Image
            {
                Width = userControl.buttonStop.Width,
                Source = new BitmapImage(new Uri($@"{resourcesPath}\stop-white.png"))
            };
            StopImageDisabled = new Image
            {
                Width = userControl.buttonStop.Width,
                Source = new BitmapImage(new Uri($@"{resourcesPath}\stop-disabled.png"))
            };

            OpenImage = new Image
            {
                Width = userControl.buttonOpen.Width,
                Source = new BitmapImage(new Uri($@"{resourcesPath}\open.png"))
            };
            OpenImageWhite = new Image
            {
                Width = userControl.buttonOpen.Width,
                Source = new BitmapImage(new Uri($@"{resourcesPath}\open-white.png"))
            };
            OpenImageDisabled = new Image
            {
                Width = userControl.buttonOpen.Width,
                Source = new BitmapImage(new Uri($@"{resourcesPath}\open-disabled.png"))
            };

            MutedImage = new Image
            {
                Width = userControl.buttonMuteUnmute.Width,
                Source = new BitmapImage(new Uri($@"{resourcesPath}\muted.png"))
            };
            MutedImageWhite = new Image
            {
                Width = userControl.buttonMuteUnmute.Width,
                Source = new BitmapImage(new Uri($@"{resourcesPath}\muted-white.png"))
            };

            HighVolumeImage = new Image
            {
                Width = userControl.buttonMuteUnmute.Width,
                Source = new BitmapImage(new Uri($@"{resourcesPath}\high-volume.png"))
            };
            HighVolumeImageWhite = new Image
            {
                Width = userControl.buttonMuteUnmute.Width,
                Source = new BitmapImage(new Uri($@"{resourcesPath}\high-volume-white.png"))
            };

            LowVolumeImage = new Image
            {
                Width = userControl.buttonMuteUnmute.Width,
                Source = new BitmapImage(new Uri($@"{resourcesPath}\low-volume.png"))
            };
            LowVolumeImageWhite = new Image
            {
                Width = userControl.buttonMuteUnmute.Width,
                Source = new BitmapImage(new Uri($@"{resourcesPath}\low-volume-white.png"))
            };

            SettingsImage = new Image
            {
                Width = userControl.buttonMuteUnmute.Width,
                Source = new BitmapImage(new Uri($@"{resourcesPath}\settings.png"))
            };
            SettingsImageWhite = new Image
            {
                Width = userControl.buttonMuteUnmute.Width,
                Source = new BitmapImage(new Uri($@"{resourcesPath}\settings-white.png"))
            };
            SettingsImageDisabled = new Image
            {
                Width = userControl.buttonMuteUnmute.Width,
                Source = new BitmapImage(new Uri($@"{resourcesPath}\settings-disabled.png"))
            };

            userControl.imagePlayPause.Source = PlayImageDisabled.Source;
            userControl.imageStop.Source = StopImageDisabled.Source;
            userControl.imageOpen.Source = OpenImage.Source;
            userControl.imageMuteUnmute.Source = HighVolumeImage.Source;
            userControl.imageSettings.Source = SettingsImage.Source;

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

            Theme theme = ThemeManager.DetectTheme(Application.Current);
            ChangeImageTheme(theme);

            ThemeManager.IsThemeChanged += ThemeManager_IsThemeChanged;
        }

        private void ThemeManager_IsThemeChanged(object sender, OnThemeChangedEventArgs e)
        {
            ChangeImageTheme(e.Theme);
        }

        private void ChangeImageTheme(Theme theme)
        {
            if (theme.BaseColorScheme == "Light")
            {
                ChangeImages(false);
                DarkTheme = false;
            }
            else
            {
                ChangeImages(true);
                DarkTheme = true;
            }
        }

        private void ChangeImages(bool dark)
        {
            if (dark)
            {
                if (userControl.imageSettings.Source != SettingsImageDisabled.Source)
                {
                    userControl.imageSettings.Source = SettingsImageWhite.Source;
                }
                if (userControl.imageStop.Source != StopImageDisabled.Source)
                {
                    userControl.imageStop.Source = StopImageWhite.Source;
                }
                if (userControl.imagePlayPause.Source != PlayImageDisabled.Source && userControl.imagePlayPause.Source != PauseImageDisabled.Source)
                {
                    if (userControl.imagePlayPause.Source == PlayImage.Source)
                    {
                        userControl.imagePlayPause.Source = PlayImageWhite.Source;
                    }
                    else
                    {
                        userControl.imagePlayPause.Source = PauseImageWhite.Source;
                    }
                }
                if (userControl.imageOpen.Source != OpenImageDisabled.Source)
                {
                    userControl.imageOpen.Source = OpenImageWhite.Source;
                }
                if (userControl.sliderVolume.Value == 0)
                {
                    userControl.imageMuteUnmute.Source = MutedImageWhite.Source;
                }
                else if (userControl.sliderVolume.Value <= .5)
                {
                    userControl.imageMuteUnmute.Source = LowVolumeImageWhite.Source;
                }
                else
                {
                    userControl.imageMuteUnmute.Source = HighVolumeImageWhite.Source;
                }
            }
            else
            {
                if (userControl.imageSettings.Source != SettingsImageDisabled.Source)
                {
                    userControl.imageSettings.Source = SettingsImage.Source;
                }
                if (userControl.imageStop.Source != StopImageDisabled.Source)
                {
                    userControl.imageStop.Source = StopImage.Source;
                }
                if (userControl.imagePlayPause.Source != PlayImageDisabled.Source && userControl.imagePlayPause.Source != PauseImageDisabled.Source)
                {
                    if (userControl.imagePlayPause.Source == PlayImageWhite.Source)
                    {
                        userControl.imagePlayPause.Source = PlayImage.Source;
                    }
                    else
                    {
                        userControl.imagePlayPause.Source = PauseImage.Source;
                    }
                }
                if (userControl.imageOpen.Source != OpenImageDisabled.Source)
                {
                    userControl.imageOpen.Source = OpenImage.Source;
                }
                if (userControl.sliderVolume.Value == 0)
                {
                    userControl.imageMuteUnmute.Source = MutedImage.Source;
                }
                else if (userControl.sliderVolume.Value <= .5)
                {
                    userControl.imageMuteUnmute.Source = LowVolumeImage.Source;
                }
                else
                {
                    userControl.imageMuteUnmute.Source = HighVolumeImage.Source;
                }
            }
        }

        public void ShowTime()
        {
            if (userControl.player.NaturalDuration.HasValue)
            {
                TimeSpan currentTime = TimeSpan.FromSeconds(userControl.sliderProgress.Value);
                userControl.textBlockDuration.Text = $"{currentTime.ToString(@"m\:ss")} / {userControl.player.NaturalDuration.Value.ToString(@"m\:ss")}";
            }
        }

        public void DisablePlayPause()
        {
            userControl.buttonPlayPause.IsEnabled = false;
            userControl.imagePlayPause.Source = PlayImageDisabled.Source;
        }

        public void EnablePlayPause()
        {
            userControl.buttonPlayPause.IsEnabled = true;

            if (DarkTheme)
            {
                userControl.imagePlayPause.Source = PauseImageWhite.Source;
            }
            else
            {
                userControl.imagePlayPause.Source = PauseImage.Source;
            }
        }

        public void DisableStop()
        {
            userControl.buttonStop.IsEnabled = false;
            userControl.imageStop.Source = StopImageDisabled.Source;
        }

        public void EnableStop()
        {
            userControl.buttonStop.IsEnabled = true;

            if (DarkTheme)
            {
                userControl.imageStop.Source = StopImageWhite.Source;
            }
            else
            {
                userControl.imageStop.Source = StopImage.Source;
            }
        }

        public void Open(string filePath)
        {
            if (userControl.player.NaturalDuration.HasValue)
            {
                Stop();
            }

            Media video = new Media(filePath);
            userControl.player.Source = video.Uri;
            Play();
        }

        public void Play()
        {
            ProgressTimer.Start();
            userControl.player.Play();

            if (DarkTheme)
            {
                userControl.imagePlayPause.Source = PauseImageWhite.Source;
            }
            else
            {
                userControl.imagePlayPause.Source = PauseImage.Source;
            }
        }

        public void Pause()
        {
            ProgressTimer.Stop();
            userControl.player.Pause();

            if (DarkTheme)
            {
                userControl.imagePlayPause.Source = PlayImageWhite.Source;
            }
            else
            {
                userControl.imagePlayPause.Source = PlayImage.Source;
            }
        }

        public void Stop(bool resetSource = true)
        {
            userControl.player.Close();
            if (resetSource)
            {
                userControl.player.Source = null;
                userControl.textBoxLoopStart.IsEnabled = false;
                userControl.textBoxLoopEnd.IsEnabled = false;
                ResetLoop();
                userControl.checkBoxLoopTime.IsEnabled = false;
                userControl.checkBoxLoopTime.IsChecked = false;
                userControl.sliderProgress.IsEnabled = false;
            }

            DisablePlayPause();
            DisableStop();

            ProgressTimer.Stop();
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

            if (userControl.player.NaturalDuration.HasValue &&
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
