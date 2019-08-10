using System.Windows.Input;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Forms;
using System;
using System.ComponentModel;
using System.Xml.Linq;
using System.Collections.Generic;
using System.Linq;

using DownloadManager.Commands;
using DownloadManager.Views;
using DownloadManager.Download;
using DownloadManager.Properties;


namespace DownloadManager.ViewModels
{

    class MainWindowViewModel : BaseViewModel
    {
        #region Private fields
        private ICommand _addDownload;
        private ICommand _addDownloadWithSelectingFolder;
        private ICommand _startDownloading;
        private ICommand _pauseDownloading;
        private ICommand _deleteDownloading;
        private ICommand _browseCommand;
        private object _selectedItem;
        private string _defaultPath;
        private int _maxDownloads;

        private ObservableCollection<WebDownloadClient> _downloads;
        #endregion

        public object SelectedItem
        {
            get { return _selectedItem; }
            set { SetProperty(ref _selectedItem, value); }
        }

        
        public ObservableCollection<WebDownloadClient> Downloads
        {
            get { return _downloads; }
            set { SetProperty(ref _downloads, value); }
        }

        public string DefaultPath
        {
            get { return _defaultPath; }
            set { SetProperty(ref _defaultPath, value); }
        }

        public int MaxDownloads
        {
            get { return _maxDownloads; }
            set
            {
                SetProperty(ref _maxDownloads, value);
                RaiseDownloadChanged();
            }
        }

        public MainWindowViewModel()
        {
            DeserializeDownloads();
            
            

            Downloads = Download.DownloadManager.Instance.DownloadList;
            DefaultPath = Settings.Default.DefaultPath;
            MaxDownloads = Settings.Default.MaxDownloads;

            MaxDownloadsChanged += MaxDownloadChangedHandler;



        }

        #region Actions

        private void AddDownloadAction()
        {
            AddUrlWindow addUrl = new AddUrlWindow();
            addUrl.ShowDialog();
            
        }

        private void PauseDownloadAction()
        {
            (SelectedItem as WebDownloadClient).Pause();
        }

        private void StartDownloadAction()
        {
            (SelectedItem as WebDownloadClient).Start();
        }

        private void DeleteDownloadAction()
        {
            WebDownloadClient delete = (SelectedItem as WebDownloadClient);
            if (delete.Status != DownloadStatus.Completed)
            {
                File.Delete(delete.DownloadPath);
                Download.DownloadManager.Instance.DownloadList.Remove(delete);
            }
            else
                Download.DownloadManager.Instance.DownloadList.Remove(delete);

            
        }

        private void AddDownloadWithSelectAction()
        {
            AddUrlSelectWindow addWindow = new AddUrlSelectWindow();
            addWindow.ShowDialog();

        }

        private void BrowseAction()
        {
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            DialogResult result = folderBrowserDialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                string path = folderBrowserDialog.SelectedPath.Replace(@"\", @"\\");
                DefaultPath = Settings.Default.DefaultPath = path.LastIndexOf(@"\\") != path.Length - 1 ? $@"{path}\\" : path;
                Settings.Default.Save();
            }


        }

        #endregion

        #region Commands and events

        public RelayCommand AddDownloadCommand
        {
            get
            {
                if (_addDownload == null)
                    _addDownload = new RelayCommand(AddDownloadAction);
                return (RelayCommand)_addDownload;
            }
        }

        public RelayCommand PauseDownloadCommand
        {
            get
            {
                if (_pauseDownloading == null)
                    _pauseDownloading = new RelayCommand(PauseDownloadAction);
                return (RelayCommand)_pauseDownloading;
            }
        }

        public RelayCommand StartDownloadCommand
        {
            get
            {
                if (_startDownloading == null)
                    _startDownloading = new RelayCommand(StartDownloadAction);
                return (RelayCommand)_startDownloading;
            }
        }

        public RelayCommand DeleteDownloadCommand
        {
            get
            {
                if (_deleteDownloading == null)
                    _deleteDownloading = new RelayCommand(DeleteDownloadAction);
                return (RelayCommand)_deleteDownloading;
            }
        }

        public RelayCommand AddDownloadWithSelect
        {
            get
            {
                if (_addDownloadWithSelectingFolder == null)
                    _addDownloadWithSelectingFolder = new RelayCommand(AddDownloadWithSelectAction);
                return (RelayCommand)_addDownloadWithSelectingFolder;
            }
        }

        public RelayCommand BrowseCommand
        {
            get
            {
                if (_browseCommand == null)
                    _browseCommand = new RelayCommand(BrowseAction);
                return (RelayCommand)_browseCommand;
            }
        }

        public event EventHandler MaxDownloadsChanged;
       

        public void MaxDownloadChangedHandler(object sender, EventArgs e)
        {
            Settings.Default.MaxDownloads = MaxDownloads;
            Settings.Default.Save();
        }

        public void RaiseDownloadChanged()
        {
            MaxDownloadsChanged?.Invoke(this, EventArgs.Empty);
        }

       

        #endregion

        public void OnClose(object sender, CancelEventArgs e)
        {
            SerializeDownloads();
        }


        private void SerializeDownloads()
        {

            if(Downloads.Count > 0)
            {
                PauseAllDownloads();

                XElement mainElement = new XElement("downloads");

                foreach(WebDownloadClient downlaod in Downloads)
                {
                    XElement element = new XElement("download",
                        new XElement("filename", downlaod.FileName),
                        new XElement("url", downlaod.UrlString),
                        new XElement("download_path", downlaod.DownloadPath),
                        new XElement("file_size", downlaod.FileSize.ToString()),
                        new XElement("downloaded_size", downlaod.DownloadedSize.ToString()),
                        new XElement("downloading_status", downlaod.Status.ToString()),
                        new XElement("downloading_status_string", downlaod.StatusString),
                        new XElement("total_time", downlaod.TotalElapsedTime.ToString()),
                        new XElement("added_on", downlaod.AddedOn.ToString()),
                        new XElement("completed_on", downlaod.CompletedOn.ToString()),
                        new XElement("supports_range", downlaod.SupportRange.ToString()),
                        new XElement("has_error", downlaod.HasError.ToString()),
                        new XElement("temp_created", downlaod.TempFileWasCreated.ToString()));

                    mainElement.Add(element);
                }

                XDocument document = new XDocument();
                document.Add(mainElement);

                document.Save("Downloads.xml");
            }


        }

        private void PauseAllDownloads()
        {
            foreach(WebDownloadClient download in Downloads)
            {
                if (download.Status != DownloadStatus.Completed)
                    download.Pause();
            }
        }

        private void DeserializeDownloads()
        {
  
            try
            {
                if (File.Exists("Downloads.xml"))
                {
                    XElement downloads = XElement.Load("Downloads.xml");

                    if (downloads.HasElements)
                    {
                        IEnumerable<XElement> downloadList = from el in downloads.Elements() select el;

                        foreach(XElement download in downloadList)
                        {
                            WebDownloadClient downloadClient = new WebDownloadClient(download.Element("url").Value, download.Element("download_path").Value, download.Element("filename").Value);
                            downloadClient.FileSize = Convert.ToInt64(download.Element("file_size").Value);
                            downloadClient.DownloadedSize = Convert.ToInt64(download.Element("downloaded_size").Value);

                            Download.DownloadManager.Instance.DownloadList.Add(downloadClient);
                            //Downloads = Download.DownloadManager.Instance.DownloadList;

                            if (download.Element("downloading_status").Value == "Completed")
                                downloadClient.Status = DownloadStatus.Completed;
                            else
                            {
                                downloadClient.Status = DownloadStatus.Paused;
                            }

                            downloadClient.StatusString = download.Element("downloading_status_string").Value;
                            downloadClient.ElapsedTime = TimeSpan.Parse(download.Element("total_time").Value);
                            downloadClient.AddedOn = DateTime.Parse(download.Element("added_on").Value);
                            downloadClient.CompletedOn = DateTime.Parse(download.Element("completed_on").Value);
                            downloadClient.SupportRange = bool.Parse(download.Element("supports_range").Value);
                            downloadClient.HasError = bool.Parse(download.Element("has_error").Value);

                            if (downloadClient.Status == DownloadStatus.Paused && !downloadClient.HasError)
                                downloadClient.Start();

                        }

                        XElement mainElement = new XElement("downloads");
                        XDocument document = new XDocument();
                        document.Add(mainElement);
                        document.Save("Downloads.xml");

                    }
                }
            }catch(Exception e)
            {
                MessageBox.Show(e.Message);
            }


        }

    }
}
