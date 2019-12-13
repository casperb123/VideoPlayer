﻿using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using VideoPlayer.Entities;

namespace VideoPlayer.UserControls.ViewModels
{
    public class MediaPlayerUserControlViewModel
    {
        private readonly MediaPlayerUserControl userControl;

        public readonly Image PlayImage;
        public readonly Image PlayImageDisabled;

        public readonly Image PauseImage;
        public readonly Image PauseImageDisabled;

        public readonly Image StopImage;
        public readonly Image StopImageDisabled;

        public readonly Image OpenImage;
        public readonly Image OpenImageDisabled;

        public readonly Image MutedImage;
        public readonly Image MutedImageDisabled;

        public readonly Image HighVolume;
        public readonly Image HighVolumeDisabled;

        public readonly Image LowVolume;
        public readonly Image LowVolumeDisabled;

        public readonly Image Settings;
        public readonly Image SettingsDisabled;

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

            userControl.mediaElementBackground.Background = new SolidColorBrush(Color.FromRgb(16, 16, 16));
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
            MutedImageDisabled = new Image
            {
                Width = userControl.buttonMuteUnmute.Width,
                Source = new BitmapImage(new Uri($@"{resourcesPath}\muted-disabled.png"))
            };

            HighVolume = new Image
            {
                Width = userControl.buttonMuteUnmute.Width,
                Source = new BitmapImage(new Uri($@"{resourcesPath}\high-volume.png"))
            };
            HighVolumeDisabled = new Image
            {
                Width = userControl.buttonMuteUnmute.Width,
                Source = new BitmapImage(new Uri($@"{resourcesPath}\high-volume-disabled.png"))
            };

            LowVolume = new Image
            {
                Width = userControl.buttonMuteUnmute.Width,
                Source = new BitmapImage(new Uri($@"{resourcesPath}\low-volume.png"))
            };
            LowVolumeDisabled = new Image
            {
                Width = userControl.buttonMuteUnmute.Width,
                Source = new BitmapImage(new Uri($@"{resourcesPath}\low-volume-disabled.png"))
            };

            Settings = new Image
            {
                Width = userControl.buttonMuteUnmute.Width,
                Source = new BitmapImage(new Uri($@"{resourcesPath}\settings.png"))
            };
            SettingsDisabled = new Image
            {
                Width = userControl.buttonMuteUnmute.Width,
                Source = new BitmapImage(new Uri($@"{resourcesPath}\settings-disabled.png"))
            };

            userControl.imagePlayPause.Source = PlayImageDisabled.Source;
            userControl.imageStop.Source = StopImageDisabled.Source;
            userControl.imageOpen.Source = OpenImage.Source;
            userControl.imageMuteUnmute.Source = HighVolume.Source;
            userControl.imageSettings.Source = Settings.Source;

            ProgressTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            ProgressTimer.Tick += (s, e) =>
            {
                if (IsPlaying)
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

        public bool IsPlaying
        {
            get
            {
                switch (GetMediaState(userControl.player))
                {
                    case MediaState.Manual:
                        return false;
                    case MediaState.Play:
                        return true;
                    case MediaState.Close:
                        return false;
                    case MediaState.Pause:
                        return false;
                    case MediaState.Stop:
                        return false;
                    default:
                        return false;
                }
            }
            set
            {
                if (value)
                {
                    Play();
                }
                else
                {
                    Pause();
                }
            }
        }

        public bool IsPaused
        {
            get
            {
                switch (GetMediaState(userControl.player))
                {
                    case MediaState.Manual:
                        return false;
                    case MediaState.Play:
                        return false;
                    case MediaState.Close:
                        return false;
                    case MediaState.Pause:
                        return true;
                    case MediaState.Stop:
                        return false;
                    default:
                        return false;
                }
            }
        }

        private MediaState GetMediaState(MediaElement mediaElement)
        {
            FieldInfo hlp = typeof(MediaElement).GetField("_helper", BindingFlags.NonPublic | BindingFlags.Instance);
            object helperObject = hlp.GetValue(mediaElement);
            FieldInfo stateField = helperObject.GetType().GetField("_currentState", BindingFlags.NonPublic | BindingFlags.Instance);
            MediaState state = (MediaState)stateField.GetValue(helperObject);
            return state;
        }

        public void ShowTime()
        {
            if (userControl.player.NaturalDuration.HasTimeSpan)
            {
                TimeSpan currentTime = TimeSpan.FromSeconds(userControl.sliderProgress.Value);
                userControl.textBlockDuration.Text = $"{currentTime.ToString(@"m\:ss")} / {userControl.player.NaturalDuration.TimeSpan.ToString(@"m\:ss")}";
            }
        }

        public void DisablePlayPause()
        {
            userControl.buttonPlayPause.IsEnabled = false;

            if (userControl.imagePlayPause.Source == PlayImage.Source)
            {
                userControl.imagePlayPause.Source = PlayImageDisabled.Source;
            }
            else if (userControl.imagePlayPause.Source == PauseImage.Source)
            {
                userControl.imagePlayPause.Source = PauseImageDisabled.Source;
            }
        }

        public void EnablePlayPause()
        {
            userControl.buttonPlayPause.IsEnabled = true;

            if (userControl.imagePlayPause.Source == PlayImageDisabled.Source)
            {
                userControl.imagePlayPause.Source = PlayImage.Source;
            }
            else if (userControl.imagePlayPause.Source == PauseImageDisabled.Source)
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
            userControl.imageStop.Source = StopImage.Source;
        }

        public void Open(string filePath)
        {
            if (userControl.player.NaturalDuration.HasTimeSpan)
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
            userControl.imagePlayPause.Source = PauseImage.Source;
        }

        public void Pause()
        {
            ProgressTimer.Stop();
            userControl.player.Pause();
            userControl.imagePlayPause.Source = PlayImage.Source;
        }

        public void Stop(bool resetSource = true)
        {
            userControl.player.Close();
            if (resetSource)
            {
                userControl.player.Source = null;
                userControl.textBoxLoopStart.IsEnabled = false;
                userControl.textBoxLoopEnd.IsEnabled = false;
                userControl.checkBoxLoopTime.IsEnabled = false;
                userControl.checkBoxLoopTime.IsChecked = false;
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
            if (timeSpan.Minutes > 0)
            {
                if (timeSpan.Minutes > 9 || timeSpan.Hours > 0)
                {
                    timeSpanString += @"mm\:";
                }
                else
                {
                    timeSpanString += @"m\:";
                }
            }

            return timeSpan.ToString($@"{timeSpanString}ss");
        }

        public void SetLoopTime(string start, string end)
        {
            Regex firstSyntax = new Regex("[0-9]:[0-59]");
            Regex secondSyntax = new Regex("[0-59]:[0-59]");
            Regex thirdSyntax = new Regex("[0-9]:[0-59]:[0-59]");

            if (userControl.player.NaturalDuration.HasTimeSpan &&
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
                }
            }
            else
            {
                loopEnd = 0;
                loopStart = 0;
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

        private double PixelsToValue(double pixels, double minValue, double maxValue, double width)
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
    }
}
