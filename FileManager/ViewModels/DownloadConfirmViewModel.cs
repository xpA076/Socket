using FileManager.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FileManager.Models.TransferLib;
using FileManager.Models.TransferLib.Info;

namespace FileManager.ViewModels
{
    public class DownloadConfirmViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private bool isDirectory;

        private string name;

        private string remoteDirectory;

        private long length = -1;


        public bool IsDirectory
        {
            get
            {
                return isDirectory;
            }
            set
            {
                isDirectory = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsDirectory"));
            }
        }

        public string Name
        {
            get
            {
                return name;
            }
            set
            {
                name = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Name"));
            }
        }

        public string RemoteDirectory
        {
            get
            {
                return remoteDirectory;
            }
            set
            {
                remoteDirectory = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("RemoteDirectory"));
            }
        }

        public string Size
        {
            get
            {
                if (length < 0) { return "querying..."; }
                if ((length / (1 << 30)) > 0)
                {
                    double size = (double)(length >> 20) / 1024;
                    return size.ToString("0.00") + " G";
                }
                else if ((length / (1 << 20)) > 0)
                {
                    double size = (double)(length >> 10) / 1024;
                    return size.ToString("0.00") + " M";
                }
                else if ((length / (1 << 10)) > 0)
                {
                    double size = (double)length / 1024;
                    return size.ToString("0.00") + " K";
                }
                else
                {
                    return length.ToString() + " B";
                }
            }
        }

        public void SetLength(long length)
        {
            this.length = length;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Size"));
        }

        public DownloadConfirmViewModel(TransferInfoDirectory directoryInfo)
        {
            this.isDirectory = true;
            this.name = directoryInfo.Name;
            this.remoteDirectory = directoryInfo.RemotePath;
            this.length = -1;
        }

        public DownloadConfirmViewModel(TransferInfoFile fileInfo)
        {
            this.isDirectory = false;
            this.name = fileInfo.Name;
            this.remoteDirectory = fileInfo.RemotePath;
            this.length = fileInfo.Length;
        }

    }
}
