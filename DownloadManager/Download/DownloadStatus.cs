using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DownloadManager.Download
{
    public enum DownloadStatus
    {
        Initialized,
        Waiting,
        Downloading,
        Pausing,
        Paused,
        Queed,
        Deleting,
        Deleted,
        Completed,
        Error
    }
}
