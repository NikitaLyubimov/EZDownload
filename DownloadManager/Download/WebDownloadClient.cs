using DownloadManager.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Threading;
using System.Windows;

namespace DownloadManager.Download
{
    [Serializable()]
    public class WebDownloadClient : INotifyPropertyChanged
    {
        #region Private filds

        private long _fileSize;
        private long _downloadedSize;

        #endregion


        #region Fields and Properties

        public string FileName { get; set; }

        public Uri Url { get; private set; }

        public string UrlString { get; set; }

        public string FileType { get; set; }

        [NonSerialized()]
        public Thread DownloadThread = null;

        public string TempDownloadPath { get; set; }

        public string DownloadPath { get; set; }

        /// <summary>
        /// Path to download folder
        /// </summary>
        public string DownloadFolder
        {
            get
            {
                return TempDownloadPath.Remove(TempDownloadPath.LastIndexOf("\\") + 1);
            }
        }

        /// <summary>
        /// Filesize and filesize string
        /// </summary>
        public long FileSize
        {
            get
            {
                return _fileSize;
            }
            set
            {
                _fileSize = value;
                RaisePropertyChanged();
                RaisePropertyChanged("FileSizeString");
            }
        }
        private string _fileSizeString;
        public string FileSizeString
        {
            get
            {
                if (_fileSizeString == string.Empty)
                    return DownloadManager.FormatSizeString(FileSize);
                return DownloadManager.FormatSizeString(FileSize);
            }
            set
            {
                _fileSizeString = value;
                RaisePropertyChanged();
            }

        }

        /// <summary>
        /// Downloadsize and downloadsize string
        /// </summary>
        public long DownloadedSize {
            get { return _downloadedSize; }
            set
            {
                _downloadedSize = value;
                RaisePropertyChanged();
                RaisePropertyChanged("DownloadedSizeString");
                
            }
        }
        public string DownloadedSizeString
        {
            get { return DownloadManager.FormatSizeString(DownloadedSize); }
        }

        /// <summary>
        /// Progress of downloading in percents
        /// </summary>
        public float Percent
        {
            get
            {
                return ((float)(DownloadedSize + CachedSize) / (float)FileSize) * 100F;
            }
        }

        /// <summary>
        /// Progress of downloading
        /// </summary>
        public string Progress
        {
            get
            {
                if (float.IsNaN(Percent))
                    return "0%";
                if (Percent == 100)
                    return "Downloaded!";
                return $"{((int)Percent).ToString()}%";
            }
        }


        /// <summary>
        /// Downloading speed and downlaoding speed string
        /// </summary>
        private int _downloadSpeed;
        public string DownloadSpeed
        {
            get
            {
                if (Status == DownloadStatus.Downloading && !HasError)
                    return DownloadManager.FormatSpeetString(_downloadSpeed);

                return string.Empty;

            }
        }

        private int _speedUpdateCount;


        /// <summary>
        /// Average download speed
        /// </summary>
        public string AverageDownloadSpeed
        {
            get
            {
                return DownloadManager.FormatSpeetString((int)Math.Floor((double)(DownloadedSize + CachedSize) / TotalElapsedTime.TotalSeconds));
            }
        }

        private List<int> _downloadRates = new List<int>();

        private int _recentAverageRate;


        /// <summary>
        /// Time left since start downlaoding
        /// </summary>
        public string TimeLeft
        {
            get
            {
                if (_recentAverageRate > 0 && Status == DownloadStatus.Downloading && !HasError)
                {
                    double secondsLeft = (FileSize - DownloadedSize + CachedSize) / _recentAverageRate;

                    TimeSpan span = TimeSpan.FromSeconds(secondsLeft);

                    return DownloadManager.FormatTimeSpanString(span);
                }

                return string.Empty;
            }
        }

        /// <summary>
        /// Status of downloading 
        /// </summary>
        private DownloadStatus _status;
        public DownloadStatus Status
        {
            get { return _status; }
            set
            {
                _status = value;
                if (_status != DownloadStatus.Deleting)
                    RaiseStatusChanged();


            }
        }

        /// <summary>
        /// Status string
        /// </summary>
        private string _statusText;
        public string StatusString
        {
            get
            {
                if (HasError)
                    return _statusText;
                else
                    return Status.ToString();
            }
            set
            {
                _statusText = value;

            }
        }

        /// <summary>
        /// Ellapsed time since starting downloading
        /// </summary>
        public TimeSpan ElapsedTime = new TimeSpan();

        private DateTime _lastStartTime;

        public TimeSpan TotalElapsedTime
        {
            get
            {
                if (Status != DownloadStatus.Downloading)
                    return ElapsedTime;
                else
                    return ElapsedTime.Add(DateTime.UtcNow - _lastStartTime);
            }
        }

        /// <summary>
        /// Total elapsed time(including where download was paused)
        /// </summary>
        public string TotalElapsedTimeString
        {
            get
            {
                return DownloadManager.FormatTimeSpanString(TotalElapsedTime);
            }
        }

        /// <summary>
        /// Time and size of downloaded file in the last calculation of speed
        /// </summary>
        private DateTime _lastNotificationTime;
        private long _lastNotificationDownloadSize;


        public DateTime LastUpdateTime { get; set; }

        /// <summary>
        /// Date and time where download was added to the list
        /// </summary>
        public DateTime AddedOn { get; set; }
        public string AddedOnString
        {
            get
            {
                string format = "dd.MM.yyyy. HH:mm:ss";
                return AddedOn.ToString(format);
            }
        }


        /// <summary>
        /// Date and time where downlod comleted
        /// </summary>
        public DateTime CompletedOn { get; set; }
        public string CompletedOnString
        {
            get
            {
                if (CompletedOn != DateTime.MinValue)
                {
                    string format = "dd.MM.yyyy. HH:mm:ss";
                    return CompletedOn.ToString(format);
                }
                else
                    return string.Empty;
            }
        }

        /// <summary>
        /// Supports rage header
        /// </summary>
        public bool SupportRange;

        /// <summary>
        /// Was an errror during the downloading
        /// </summary>
        public bool HasError { get; set; }

        /// <summary>
        /// Open file when it complete downloading
        /// </summary>
        public bool OpenFileOnCompletion { get; set; }

        /// <summary>
        /// Temporary file was created
        /// </summary>
        public bool TempFileWasCreated { get; set; }

        /// <summary>
        /// File is selected
        /// </summary>
        public bool IsSelected { get; set; }

        /// <summary>
        /// Changed speed limit 
        /// </summary>
        public bool SpeedLimitChanged { get; set; }

        /// <summary>
        /// Download buffer per notification
        /// </summary>
        public int BufferCountPerNotification { get; set; }

        /// <summary>
        /// Buffer size 
        /// </summary>
        public int BufferSize { get; set; }

        /// <summary>
        /// Size of downloading cache
        /// </summary>
        public int CachedSize { get; set; }

        /// <summary>
        /// Maximum downloading cache size
        /// </summaryI
        public int MaxCacheSize { get; set; }

        private NumberFormatInfo _numberFormat = NumberFormatInfo.InvariantInfo;

        /// <summary>
        /// File locker thread while open file
        /// </summary>
        private static object _fileLocker = new object();


        #endregion

        #region Constructor and Events


        public WebDownloadClient(string url, string path, string fileName)
        {
            BufferSize = 1024;
            MaxCacheSize = Settings.Default.MemoryCacheSize * 1024;
            BufferCountPerNotification = 64;

            Url = new Uri(url, UriKind.Absolute);
            UrlString = url;
            TempDownloadPath = path;
            FileName = fileName;
            DownloadPath = $"{TempDownloadPath}{FileName}";

            _statusText = string.Empty;

            Status = DownloadStatus.Initialized;

            DownloadCompleted += DownloadCompletedHandler;
            DownloadProgressChanged += DownloadProgressChangedHandler;


        }

        public WebDownloadClient() { }

        public event  PropertyChangedEventHandler PropertyChanged;

        public event EventHandler StatusChanged;

        public event EventHandler DownloadProgressChanged;

        public event EventHandler DownloadCompleted;

        #endregion

        #region Event Handlers

        protected void RaisePropertyChanged([CallerMemberName] string name = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        protected virtual void RaiseStatusChanged()
        {
            StatusChanged?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void RaiseDownloadProgressChanged()
        {
            DownloadProgressChanged?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void RaiseDownloadCompleted()
        {
            DownloadCompleted?.Invoke(this, EventArgs.Empty);
        }

        public void DownloadProgressChangedHandler(object sender, EventArgs e)
        {
            if (DateTime.UtcNow > LastUpdateTime.AddSeconds(1))
            {
                CalcuclateDownloadSpeed();
                CalculateAverageRate();
                UpdateDownlpadDisplay();
                LastUpdateTime = DateTime.UtcNow;
            }
                
        }

        public void DownloadCompletedHandler(object sender, EventArgs e)
        {
            if (!HasError)
            {

                if (File.Exists(DownloadPath))
                {
                    
                    string normPathSec = $@"{DownloadPath}{FileType}";
                    File.Move(DownloadPath, normPathSec);
                }
                    //File.Move(TempDownloadPath, DownloadPath);

                Status = DownloadStatus.Completed;
                UpdateDownlpadDisplay();

                if (OpenFileOnCompletion && File.Exists(DownloadPath))
                    Process.Start(@DownloadPath);

            }

            else
            {
                Status = DownloadStatus.Error;
                UpdateDownlpadDisplay();

            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Check url an set file info
        /// </summary>
        public void CheckUrl()
        {
            try
            {
                var webRequest = (HttpWebRequest)WebRequest.Create(Url);
                webRequest.Method = "HEAD";
                webRequest.Timeout = 5000;

                webRequest.Credentials = CredentialCache.DefaultCredentials;

               
                
                webRequest.Proxy = WebRequest.DefaultWebProxy;

                using (WebResponse response = webRequest.GetResponse())
                {
                    foreach (var header in response.Headers.AllKeys)
                    {
                        if (header.Equals("Accept-Ranges", StringComparison.OrdinalIgnoreCase))
                            SupportRange = true;

                    }

                    string extenssion = Path.GetExtension(UrlString);

                    if (!string.IsNullOrEmpty(extenssion) && !(extenssion.Length > 5))
                    {
                        FileType = extenssion.Contains("?") || extenssion.Contains("=") ? extenssion.Substring(0, extenssion.LastIndexOf("?")) : extenssion;
                    }
                    else
                    {
                        for (int i = 0; i < response.Headers.Count; i++)
                        {
                            if (response.Headers.GetKey(i).Equals("Content-Type", StringComparison.InvariantCultureIgnoreCase))
                            {
                                string contentType = response.Headers.GetValues(i)[0];
                                FileType = $".{contentType.Substring(contentType.LastIndexOf("/") + 1)}";
                            }
                        }
                    }

      

                    FileSize = response.ContentLength;


                    if (FileSize <= 0)
                    {
                        MessageBox.Show("The request file doesn't exist");
                        HasError = true;
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show($"Error while getting file info\n({e.Message})");
                Console.WriteLine($"{e.Message}");
                HasError = true;
            }
        }




        /// <summary>
        /// Create temp file and allocate memory fo file
        /// </summary>
        private void CreateTempFile()
        {
            lock (_fileLocker)
            {
                using (FileStream fileStream = File.Create($"{TempDownloadPath}{FileName}"))
                {
                    long createdSize = 0;
                    byte[] buffer = new byte[4096];
                    while (createdSize < FileSize)
                    {
                        int bufferSize = (FileSize - createdSize) < 4096 ? (int)(FileSize - createdSize) : 4096;
                        fileStream.Write(buffer, 0, bufferSize);
                        createdSize += bufferSize;
                    }

                }
            }
        }

        private void WriteCacheToFile(MemoryStream downloadCache, int cacheSize)
        {
            lock (_fileLocker)
            {
                using (FileStream fileStream = new FileStream($"{TempDownloadPath}{FileName}", FileMode.Open))
                {
                    byte[] cacheContent = new byte[cacheSize];
                    downloadCache.Seek(0, SeekOrigin.Begin);
                    downloadCache.Read(cacheContent, 0, cacheSize);
                    fileStream.Seek(DownloadedSize, SeekOrigin.Begin);
                    fileStream.Write(cacheContent, 0, cacheSize);


                }
            }
        }

        /// <summary>
        /// Calculate download speed
        /// </summary>
        private void CalcuclateDownloadSpeed()
        {
            DateTime now = DateTime.UtcNow;
            TimeSpan interval = now - _lastNotificationTime;
            double timeDiff = interval.TotalSeconds;
            double sizeDiff = (double)(DownloadedSize + CachedSize - _lastNotificationDownloadSize);

            _downloadSpeed = (int)Math.Floor(sizeDiff / timeDiff);

            _downloadRates.Add(_downloadSpeed);

            _lastNotificationDownloadSize = DownloadedSize + MaxCacheSize;
            _lastNotificationTime = now;
        }

        private void CalculateAverageRate()
        {
            if (_downloadRates.Count > 0)
            {
                if (_downloadRates.Count > 10)
                    _downloadRates.RemoveAt(0);

                int rateSum = 0;
                _recentAverageRate = 0;
                foreach (int rate in _downloadRates)
                    rateSum += rate;

                _recentAverageRate = rateSum / _downloadRates.Count;
            }
        }

        private void UpdateDownlpadDisplay()
        {
            TimeSpan startInterval = DateTime.UtcNow - _lastStartTime;
            if (_speedUpdateCount == 0 || startInterval.TotalSeconds < 4 || HasError || Status == DownloadStatus.Paused || Status == DownloadStatus.Queed || Status == DownloadStatus.Completed)
                RaisePropertyChanged("DownloadSpeed");

            _speedUpdateCount++;

            if (_speedUpdateCount == 4)
                _speedUpdateCount = 0;

            RaisePropertyChanged("TimeLeft");
            RaisePropertyChanged("StatusString");
            RaisePropertyChanged("CompletedOnString");
            RaisePropertyChanged("Progress");

            if (IsSelected)
                RaisePropertyChanged("AverageSpeedAndTotalTime");
        }

        private void ResetProeprties()
        {
            HasError = false;
            TempFileWasCreated = false;
            DownloadedSize = 0;
            CachedSize = 0;
            _speedUpdateCount = 0;
            _recentAverageRate = 0;
            _downloadRates.Clear();
            ElapsedTime = new TimeSpan();
            CompletedOn = DateTime.MinValue;

        }

        public void Start()
        {
            if(Status == DownloadStatus.Initialized || Status == DownloadStatus.Paused || Status == DownloadStatus.Queed || HasError)
            {
                if(!SupportRange && DownloadedSize > 0)
                {
                    StatusString = "Error: Server doesn't support resume";
                    HasError = true;
                    RaiseDownloadCompleted();
                    return;
                }
                
                HasError = false;
                Status = DownloadStatus.Waiting;
                RaisePropertyChanged("StatusString");

                if (DownloadManager.Instance.ActiveDownloads > Settings.Default.MaxDownloads)
                {
                    Status = DownloadStatus.Queed;
                    RaisePropertyChanged("StatusString");
                    return;

                }

                DownloadThread = new Thread(new ThreadStart(DownloadFile));
                DownloadThread.IsBackground = true;
                DownloadThread.Start();
                
            }
        }

        public void Pause()
        {
            if (Status == DownloadStatus.Waiting || Status == DownloadStatus.Downloading)
                Status = DownloadStatus.Pausing;
            if(Status == DownloadStatus.Queed)
            {
                Status = DownloadStatus.Paused;
                RaisePropertyChanged("StatusString");
            }

        }

        public void Restart()
        {
            if(HasError || Status == DownloadStatus.Completed)
            {
                if (File.Exists(TempDownloadPath))
                    File.Delete(TempDownloadPath);

                if (File.Exists(DownloadPath))
                    File.Delete(DownloadPath);

                ResetProeprties();

                Status = DownloadStatus.Waiting;
                UpdateDownlpadDisplay();

                if(DownloadManager.Instance.ActiveDownloads > Settings.Default.MaxDownloads)
                {
                    Status = DownloadStatus.Queed;
                    RaisePropertyChanged("StatusString");
                    return;
                }

                DownloadThread = new Thread(new ThreadStart(DownloadFile));
                DownloadThread.IsBackground = true;
                DownloadThread.Start();
            }
        }

        private void DownloadFile()
        {
            HttpWebRequest webRequest = null;
            HttpWebResponse webResponce = null;
            Stream responceStream = null;
            ThrottleStream throttleStream = null;
            MemoryStream downloadCache = null;
            _speedUpdateCount = 0;
            _recentAverageRate = 0;
            if (_downloadRates.Count > 0)
                _downloadRates.Clear();

            try
            {

                
                
                CheckUrl();
                if (HasError)
                {
                    RaiseDownloadCompleted();
                    return;
                }
                
                if (!TempFileWasCreated)
                {
                    CreateTempFile();
                    TempFileWasCreated = true;
                }

                
                  

                _lastStartTime = DateTime.UtcNow;

                if (Status == DownloadStatus.Waiting)
                {
                    Status = DownloadStatus.Downloading;
                    AddedOn = DateTime.UtcNow;
                }

                webRequest = (HttpWebRequest)WebRequest.Create(Url);
                webRequest.Method = "GET";

                webRequest.Credentials = CredentialCache.DefaultCredentials;

                webRequest.Proxy = WebRequest.DefaultWebProxy;

                webRequest.AddRange(DownloadedSize);

                webResponce = (HttpWebResponse)webRequest.GetResponse();
                responceStream = webResponce.GetResponseStream();

                responceStream.ReadTimeout = 5000;

                long maxBytePerSeconds = 0;
                if (Settings.Default.EnableSpeedLimit)
                    maxBytePerSeconds = (long)((Settings.Default.SpeedLimit * 1024) / DownloadManager.Instance.ActiveDownloads);
                else
                    maxBytePerSeconds = ThrottleStream.Infinite;

                throttleStream = new ThrottleStream(responceStream, maxBytePerSeconds);

                downloadCache = new MemoryStream(MaxCacheSize);

                byte[] downloadBuffer = new byte[BufferSize];

                int byteSize = 0;

                CachedSize = 0;

                int receiveBufferCount = 0;

                while (true)
                {
                    if (SpeedLimitChanged)
                    {
                        if (Settings.Default.EnableSpeedLimit)
                            maxBytePerSeconds = (long)((Settings.Default.SpeedLimit * 1024) / DownloadManager.Instance.DownloadList.Count);
                        else
                            maxBytePerSeconds = ThrottleStream.Infinite;

                        throttleStream.MaximumBytesPerSeconds = maxBytePerSeconds;
                        SpeedLimitChanged = false;
                    }


                    byteSize = throttleStream.Read(downloadBuffer, 0, downloadBuffer.Length);

                    if(Status != DownloadStatus.Downloading || byteSize == 0 || MaxCacheSize < CachedSize + byteSize)
                    {
                        WriteCacheToFile(downloadCache, CachedSize);

                        DownloadedSize += CachedSize;

                        downloadCache.Seek(0, SeekOrigin.Begin);

                        CachedSize = 0;

                        if (Status != DownloadStatus.Downloading || byteSize == 0)
                            break;

                    }

                    downloadCache.Write(downloadBuffer, 0, byteSize);
                    CachedSize += byteSize;

                    receiveBufferCount++;

                    if(receiveBufferCount == BufferCountPerNotification)
                    {
                        RaiseDownloadProgressChanged();
                        receiveBufferCount = 0;
                    }



                }

                ElapsedTime = ElapsedTime.Add(DateTime.UtcNow - _lastStartTime);

                if(Status != DownloadStatus.Deleting)
                {
                    if (Status == DownloadStatus.Pausing)
                    {
                        Status = DownloadStatus.Paused;
                        UpdateDownlpadDisplay();
                    }
                    else if (Status == DownloadStatus.Queed)
                        UpdateDownlpadDisplay();
                    else
                    {
                        CompletedOn = DateTime.UtcNow;
                        RaiseDownloadCompleted();
                    }
                }


            }catch(Exception ex)
            {
                StatusString = $"Error: {ex.Message}";
                HasError = true;
                RaiseDownloadCompleted();
            }
            finally
            {
                if (responceStream != null)
                    responceStream.Close();
                if (throttleStream != null)
                    throttleStream.Close();
                if (webResponce != null)
                    webResponce.Close();
                if (downloadCache != null)
                    downloadCache.Close();
                if (DownloadThread != null)
                {

                    DownloadThread.Abort();

                }

            }

        }

        #endregion
    }
}
