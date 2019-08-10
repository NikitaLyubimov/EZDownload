using System;
using System.Windows;
using System.Windows.Input;

using DownloadManager.Commands;
using DownloadManager.Download;
using DownloadManager.Properties;

namespace DownloadManager.ViewModels
{
    class AddUrlWindowViewModel : BaseViewModel
    {
        #region Private fields

        private ICommand _add;
        private ICommand _cancel;

        private Window _addWindow;
        private string _url;
        public string _fileName;
        private bool _isEnabled;
        #endregion




        #region Constructor

        public AddUrlWindowViewModel(Window addWindow)
        {
            _addWindow = addWindow;
        }

        #endregion

        public string URL
        {
            get { return _url; }
            set
            {
                SetProperty(ref _url, value);
            }
        }


        public string FileName
        {
            get { return _fileName; }
            set { SetProperty(ref _fileName, value); }
        }

        #region Actions

        private void AddAction()
        {
            WebDownloadClient download = new WebDownloadClient(URL, @Settings.Default.DefaultPath, FileName);

            download.Start();
            Download.DownloadManager.Instance.DownloadList.Add(download);

            _addWindow.Close();
            
        }

        private void CancelAction()
        {
            _addWindow.Close();
        }

        #endregion

        #region Commands

        public RelayCommand AddCommand
        {
            get
            {
                if (_add == null)
                    _add = new RelayCommand(AddAction);
                return (RelayCommand)_add;
            }
        }

        public RelayCommand CancelCommand
        {
            get
            {
                if (_cancel == null)
                    _cancel = new RelayCommand(CancelAction);
                return (RelayCommand)_cancel;
            }
        } 

        #endregion



    }
}
