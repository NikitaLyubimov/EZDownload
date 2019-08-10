using DownloadManager.Commands;
using DownloadManager.Download;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;

namespace DownloadManager.ViewModels
{
    class AddUrlWithSelectViewModel : BaseViewModel
    {
        #region Private fields
        private string _folderPath;
        private string _url;
        private string _fileName;
        private Window _addWindow;
        private ICommand _browse;
        private ICommand _cancel;
        private ICommand _add;
        #endregion

        #region Constructor and public fields

        public AddUrlWithSelectViewModel(Window addWindow)
        {
            _addWindow = addWindow;
        }

        public string FolderPath
        {
            get { return _folderPath; }
            set { SetProperty(ref _folderPath, value); }
        }

        public string URL
        {
            get { return _url; }
            set { SetProperty(ref _url, value); }
        }

        public string FileName
        {
            get { return _fileName; }
            set { SetProperty(ref _fileName, value); }
        }


        #endregion

        #region Actions

        private void BrowseAction()
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            DialogResult result = fbd.ShowDialog();
            if (result == DialogResult.OK)
                FolderPath = fbd.SelectedPath;
            
            
            


        }

        private void AddAction()
        {
            WebDownloadClient download = new WebDownloadClient(URL, FolderPath, FileName);

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

        public RelayCommand CancelCommand
        {
            get
            {
                if (_cancel == null)
                    _cancel = new RelayCommand(CancelAction);
                return (RelayCommand)_cancel;
            }
        }

        public RelayCommand AddCommand
        {
            get
            {
                if (_add == null)
                    _add = new RelayCommand(AddAction);
                return (RelayCommand)_add;
            }
        }

        public RelayCommand BrowseCommand
        {
            get
            {
                if (_browse == null)
                    _browse = new RelayCommand(BrowseAction);
                return (RelayCommand)_browse;
            }
        }



        #endregion

    }
}
