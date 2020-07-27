using System;
using System.Collections.Generic;
using System.Text;

namespace GitHubUpdater
{
    public class DownloadProgressEventArgs : EventArgs
    {
        public long BytesReceived { get; private set; }
        public long TotalBytesToReceive { get; private set; }
        public int ProgressPercent { get; private set; }
        public string TimeLeft { get; private set; }
        public string TimeSpent { get; private set; }
        public string Downloaded { get; private set; }
        public string ToDownload { get; private set; }

        public DownloadProgressEventArgs(long bytesReceived, long totalBytesToReceive, int progressPercent, string timeLeft, string timeSpent, string downloaded, string toDownload)
        {
            BytesReceived = bytesReceived;
            TotalBytesToReceive = totalBytesToReceive;
            ProgressPercent = progressPercent;
            TimeLeft = timeLeft;
            TimeSpent = timeSpent;
            Downloaded = downloaded;
            ToDownload = toDownload;
        }
    }
}
