using System;
using System.Collections.ObjectModel;
using System.Globalization;


namespace DownloadManager.Download
{
    class DownloadManager
    {
        private static DownloadManager _instanse = new DownloadManager();

        public static DownloadManager Instance
        {
            get { return _instanse; }
        }

        private static NumberFormatInfo _numberFormat = NumberFormatInfo.InvariantInfo;

        public ObservableCollection<WebDownloadClient> DownloadList = new ObservableCollection<WebDownloadClient>();

        #region Properties

        public int ActiveDownloads
        {
            get
            {
                int active = 0;
                foreach(WebDownloadClient client in DownloadList)
                {
                    if (!client.HasError)
                        if (client.Status == DownloadStatus.Waiting || client.Status == DownloadStatus.Downloading)
                            active++;
                }
                return active;
            }
        }

        public int CompletedDownloads
        {
            get
            {
                int completed = 0;
                foreach(WebDownloadClient client in DownloadList)
                {
                    if (client.Status == DownloadStatus.Completed)
                        completed++;
                }
                return completed;

            }
        }

        public int TotalDownloads
        {
            get
            {
                return DownloadList.Count;
            }
        }

        #endregion

        #region Methods

        public static string FormatSizeString(long byteSize)
        {
            double killoBytes = (double)byteSize / 1024D;
            double megaBytes = killoBytes / 1024D;
            double gigaBytes = megaBytes / 1024D;

            if (byteSize < 1024)
            {
                return string.Format(_numberFormat, "{0}B", byteSize);
            }
            else if (byteSize < 1048576)
                return string.Format(_numberFormat, "{0:0.00} kB", killoBytes);
            else if (byteSize < 1073741824)
                return string.Format(_numberFormat, "{0:0.00} MB", megaBytes);
            else
                return string.Format(_numberFormat, "{0:0.00} GB", gigaBytes);
        }

        public static string FormatSpeetString(int speed)
        {
            float kbSpeed = (float)speed / 1024F;
            float mbSpeed = kbSpeed / 1024F;

            if (speed <= 0)
                return string.Empty;
            else if (speed < 1024)
                return $"{speed.ToString()} B/s";
            else if (speed < 1048576)
                return $"{kbSpeed.ToString("#.00", _numberFormat)} kB/s";
            else
                return $"{mbSpeed.ToString("#.00", _numberFormat)} MB/s";

        }

        public static string FormatTimeSpanString(TimeSpan span)
        {
            string hours = ((int)span.TotalHours).ToString();
            string minutes = span.Minutes.ToString();
            string seconds = span.Seconds.ToString();

            if ((int)span.TotalHours < 10)
                hours = $"0 {hours}";
            if (span.Minutes < 10)
                minutes = $"0 {minutes}";
            if (span.Seconds < 10)
                seconds = $"0 {seconds}";

            return $"{hours}:{minutes}:{seconds}";
        }

        #endregion

    }
}
