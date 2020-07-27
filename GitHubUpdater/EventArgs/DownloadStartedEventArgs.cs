using System;
using System.Collections.Generic;
using System.Text;

namespace GitHubUpdater
{
    public class DownloadStartedEventArgs : EventArgs
    {
        public Version Version { get; private set; }

        public DownloadStartedEventArgs(Version version)
        {
            Version = version;
        }
    }
}
