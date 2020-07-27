using System;
using System.Collections.Generic;
using System.Text;

namespace GitHubUpdater
{
    public enum UpdaterState
    {
        Idle,
        CheckingForUpdates,
        Downloading,
        Installing,
        RollingBack
    }
}
