using NAudio.Wave;
using SoundTouch.Net.NAudioSupport;
using System;

namespace VideoPlayer
{
    public sealed class SoundProcessor : IDisposable
    {
        private IWavePlayer? waveOut;

        public event EventHandler<bool> PlaybackStopped = (_, __) => { };

        public SoundTouchWaveStream? ProcessorStream { get; private set; }

        public double Volume
        {
            get
            {
                if (waveOut is null)
                    return 0;

                return waveOut.Volume;
            }
            set
            {
                if (waveOut != null)
                    waveOut.Volume = (float)value;
            }
        }

        public bool Play()
        {
            if (waveOut is null)
                return false;

            if (waveOut.PlaybackState != PlaybackState.Playing)
                waveOut.Play();

            return true;
        }

        public bool Pause()
        {
            if (waveOut is null)
                return false;

            if (waveOut.PlaybackState == PlaybackState.Playing)
            {
                waveOut.Stop();
                return true;
            }

            return false;
        }

        public bool Stop()
        {
            if (waveOut is null || ProcessorStream is null)
                return false;

            waveOut.Stop();
            ProcessorStream.Position = 0;
            ProcessorStream.Flush();
            return true;
        }

        public bool OpenFile(string filePath)
        {
            Close();

            try
            {
                MediaFoundationReader reader = new MediaFoundationReader(filePath);
                WaveChannel32 inputStream = new WaveChannel32(reader) { PadWithZeroes = false };

                ProcessorStream = new SoundTouchWaveStream(inputStream);
                waveOut = new WaveOutEvent() { DesiredLatency = 100 };

                waveOut.Init(ProcessorStream);
                waveOut.PlaybackStopped += OnPlaybackStopped;

                return true;
            }
            catch (Exception)
            {
                waveOut = null;
                return false;
            }
        }


        public void Close()
        {
            ProcessorStream?.Dispose();
            ProcessorStream = null;

            waveOut?.Dispose();
            waveOut = null;
        }

        public void Dispose() => Close();

        private void OnPlaybackStopped(object sender, StoppedEventArgs e)
        {
            bool reachedEnding = ProcessorStream is null || ProcessorStream.Position >= ProcessorStream.Length;
            if (reachedEnding)
                Stop();

            PlaybackStopped(sender, reachedEnding);
        }
    }
}
