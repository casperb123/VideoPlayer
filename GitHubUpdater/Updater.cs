using Octokit;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;

namespace GitHubUpdater
{
    public class Updater : IDisposable
    {
        public event EventHandler<VersionEventArgs> UpdateAvailable;
        public event EventHandler<DownloadStartedEventArgs> DownloadingStarted;
        public event EventHandler<DownloadProgressEventArgs> DownloadingProgressed;
        public event EventHandler<VersionEventArgs> DownloadingCompleted;
        public event EventHandler InstallationStarted;
        public event EventHandler<ExceptionEventArgs<Exception>> InstallationFailed;
        public event EventHandler<VersionEventArgs> InstallationCompleted;

        private string gitHubUsername;
        private string gitHubRepositoryName;
        private readonly WebClient webClient;
        private readonly GitHubClient gitHubClient;
        private readonly string downloadPath;
        private readonly string originalFilePath;
        private readonly string backupFilePath;
        private readonly string changelogFilePath;
        private Release release;
        private readonly Version currentVersion;
        private Version latestVersion;
        private string changelog;
        private DateTime updateStartTime;

        public string GitHubRepositoryName
        {
            get { return gitHubRepositoryName; }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new NullReferenceException("The repository name can't be null or whitespace");

                gitHubRepositoryName = value;
            }
        }

        public string GitHubUsername
        {
            get { return gitHubUsername; }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new NullReferenceException("The github username can't be null or whitespace");

                gitHubUsername = value;
            }
        }

        public UpdaterState State { get; private set; }

        public Updater(string gitHubUsername, string gitHubRepositoryName)
        {
            GitHubUsername = gitHubUsername;
            GitHubRepositoryName = gitHubRepositoryName;

            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string mainProjectName = Assembly.GetEntryAssembly().GetName().Name;
            string appDataPath = $@"{appData}\{mainProjectName}";

            if (!Directory.Exists(appDataPath))
                Directory.CreateDirectory(appDataPath);

            gitHubClient = new GitHubClient(new ProductHeaderValue(mainProjectName));
            webClient = new WebClient();
            webClient.DownloadFileCompleted += WebClient_DownloadFileCompleted;
            webClient.DownloadProgressChanged += WebClient_DownloadProgressChanged;

            originalFilePath = Process.GetCurrentProcess().MainModule.FileName;
            string appDataFilePath = $@"{appDataPath}\{Path.GetFileNameWithoutExtension(originalFilePath)}";

            backupFilePath = $"{appDataFilePath}.backup";
            downloadPath = $"{appDataFilePath}.update";
            changelogFilePath = $"{appDataFilePath}.changelog";

            if (!Directory.Exists(appDataPath))
                Directory.CreateDirectory(appDataPath);
            if (File.Exists(backupFilePath))
                File.Delete(backupFilePath);

            currentVersion = Version.ConvertToVersion(Assembly.GetEntryAssembly().GetName().Version.ToString());
        }

        public Updater(string gitHubUsername, string gitHubRepositoryName, bool rollBackOnFail) : this(gitHubUsername, gitHubRepositoryName)
        {
            if (rollBackOnFail)
                InstallationFailed += (s, e) => Rollback();
        }

        private void WebClient_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            string received = string.Format(CultureInfo.InvariantCulture, "{0:n0} kb", e.BytesReceived / 1000);
            string toReceive = string.Format(CultureInfo.InvariantCulture, "{0:n0} kb", e.TotalBytesToReceive / 1000);

            if (e.BytesReceived / 1000000 >= 1)
                received = string.Format("{0:.#0} MB", Math.Round((decimal)e.BytesReceived / 1000000, 2));
            if (e.TotalBytesToReceive / 1000000 >= 1)
                toReceive = string.Format("{0:.#0} MB", Math.Round((decimal)e.TotalBytesToReceive / 1000000, 2));

            TimeSpan timeSpent = DateTime.Now - updateStartTime;
            int secondsRemaining = (int)(timeSpent.TotalSeconds / e.ProgressPercentage * (100 - e.ProgressPercentage));
            TimeSpan timeLeft = new TimeSpan(0, 0, secondsRemaining);
            string timeLeftString = string.Empty;
            string timeSpentString = string.Empty;

            if (timeLeft.Hours > 0)
                timeLeftString += string.Format("{0} hours", timeLeft.Hours);
            if (timeLeft.Minutes > 0)
                timeLeftString += timeLeftString == string.Empty ? string.Format("{0} min", timeLeft.Minutes) : string.Format(" {0} min", timeLeft.Minutes);
            if (timeLeft.Seconds >= 0)
                timeLeftString += timeLeftString == string.Empty ? string.Format("{0} sec", timeLeft.Seconds) : string.Format(" {0} sec", timeLeft.Seconds);

            if (timeSpent.Hours > 0)
                timeSpentString = string.Format("{0} hours", timeSpent.Hours);
            if (timeSpent.Minutes > 0)
                timeSpentString += timeSpentString == string.Empty ? string.Format("{0} min", timeSpent.Minutes) : string.Format(" {0} min", timeSpent.Minutes);
            if (timeSpent.Seconds >= 0)
                timeSpentString += timeSpentString == string.Empty ? string.Format("{0} sec", timeSpent.Seconds) : string.Format(" {0} sec", timeSpent.Seconds);

            DownloadingProgressed?.Invoke(this, new DownloadProgressEventArgs(e.BytesReceived, e.TotalBytesToReceive, e.ProgressPercentage, timeLeftString, timeSpentString, received, toReceive));
        }

        private void WebClient_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            State = UpdaterState.Idle;

            File.WriteAllText(changelogFilePath, changelog);
            DownloadingCompleted?.Invoke(this, new VersionEventArgs(currentVersion, latestVersion, false, changelog));
        }

        public async Task<(bool updateAvailable, Version latestVersion)> CheckForUpdatesAsync()
        {
            State = UpdaterState.CheckingForUpdates;
            Release release = null;

            if (File.Exists(downloadPath))
            {
                FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(downloadPath);
                latestVersion = Version.ConvertToVersion(fileVersionInfo.FileVersion);

                if (latestVersion > currentVersion)
                {
                    if (File.Exists(changelogFilePath))
                        UpdateAvailable?.Invoke(this, new VersionEventArgs(currentVersion, latestVersion, true, File.ReadAllText(changelogFilePath)));
                    else
                        UpdateAvailable?.Invoke(this, new VersionEventArgs(currentVersion, latestVersion, true));

                    State = UpdaterState.Idle;
                    return (true, latestVersion);
                }
            }
            else
            {
                var releases = await gitHubClient.Repository.Release.GetAll(GitHubUsername, GitHubRepositoryName);
                release = releases[0];
                latestVersion = Version.ConvertToVersion(release.TagName.Replace("v", ""));

                if (latestVersion > currentVersion)
                {
                    this.release = release;
                    changelog = release.Body;
                    UpdateAvailable?.Invoke(this, new VersionEventArgs(currentVersion, latestVersion, false, release.Body));
                    State = UpdaterState.Idle;
                    return (true, latestVersion);
                }
            }

            State = UpdaterState.Idle;
            return (false, currentVersion);
        }

        public bool IsUpdateDownloaded()
        {
            if (File.Exists(downloadPath))
                return true;

            return false;
        }

        public void DownloadUpdate()
        {
            if (release is null)
                throw new NullReferenceException("There isn't any update available");
            if (!release.Assets[0].Name.EndsWith(".exe"))
            {
                string extension = Path.GetExtension(release.Assets[0].Name);
                throw new FileLoadException($"The downloaded file is a {extension} file, which is not supported");
            }

            if (File.Exists(downloadPath))
                File.Delete(downloadPath);

            DownloadingStarted?.Invoke(this, new DownloadStartedEventArgs(latestVersion));
            State = UpdaterState.Downloading;

            updateStartTime = DateTime.Now;
            webClient.DownloadFileAsync(new Uri(release.Assets[0].BrowserDownloadUrl), downloadPath);
        }

        public void InstallUpdate()
        {
            State = UpdaterState.Installing;
            InstallationStarted?.Invoke(this, EventArgs.Empty);

            try
            {
                if (File.Exists(backupFilePath))
                    File.Delete(backupFilePath);

                File.Move(originalFilePath, backupFilePath);
                File.Move(downloadPath, originalFilePath);
            }
            catch (Exception ex)
            {
                InstallationFailed?.Invoke(this, new ExceptionEventArgs<Exception>(ex, ex.Message));
                return;
            }

            State = UpdaterState.Idle;
            InstallationCompleted?.Invoke(this, new VersionEventArgs(currentVersion, latestVersion));
        }

        public void Restart()
        {
            Process.Start(originalFilePath);
            Environment.Exit(0);
        }

        public void Rollback()
        {
            if (File.Exists(backupFilePath))
            {
                State = UpdaterState.RollingBack;
                File.Move(backupFilePath, originalFilePath, true);
                State = UpdaterState.Idle;
            }
            else
                throw new FileNotFoundException("The backup file was not found");
        }

        public void Dispose()
        {
            webClient.Dispose();
        }
    }
}
