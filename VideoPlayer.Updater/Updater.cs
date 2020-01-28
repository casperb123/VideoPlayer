using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using GitHubUpdate;

namespace VideoPlayer.Updater
{
    public class Updater
    {
        private readonly string userName;
        private readonly string repositoryName;
        private readonly UpdateChecker updateChecker;

        public Updater(string userName, string repositoryName)
        {
            this.userName = userName;
            this.repositoryName = repositoryName;
            updateChecker = new UpdateChecker("casperb123", "VideoPlayer");
        }

        public async Task<bool> CheckForUpdates()
        {
            UpdateType update = await updateChecker.CheckUpdate();
            if (update == UpdateType.None)
                return false;

            return true;
        }
    }
}
