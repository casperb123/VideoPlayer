using System;
using System.Collections.Generic;
using System.Text;

namespace GitHubUpdater
{
    public class VersionEventArgs : EventArgs
    {
        public Version CurrentVersion { get; private set; }
        public Version LatestVersion { get; private set; }
        public bool UpdateDownloaded { get; private set; }
        public string Changelog { get; private set; }

        public VersionEventArgs(Version currentVersion, Version latestVersion)
        {
            CurrentVersion = currentVersion;
            LatestVersion = latestVersion;
        }

        public VersionEventArgs(Version currentVersion, Version latestVersion, bool updateDownloaded) : this(currentVersion, latestVersion)
        {
            UpdateDownloaded = updateDownloaded;
        }

        public VersionEventArgs(Version currentVersion, Version latestVersion, bool updateDownloaded, string changelog) : this(currentVersion, latestVersion, updateDownloaded)
        {
            Changelog = changelog;
        }
    }
}
