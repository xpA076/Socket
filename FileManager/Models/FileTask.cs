using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

using SocketLib;

namespace FileManager.Models
{
    public enum FileTaskStatus : int
    {
        Waiting,
        Pause,
        Success,
        Denied,
        Failed,
        Downloading,
        Uploading,
        Sizing
    }

    public enum FileTaskType
    {
        Download, 
        Upload
    }


    public class FileTask : INotifyPropertyChanged
    {
        public TCPAddress TcpAddress { get; set; }

        public bool IsDirectory { get; set; } = false;
        public FileTaskType Type { get; set; }
        public string RemotePath { get; set; }
        public string LocalPath { get; set; }


        private long _length = -1;
        public long Length
        {
            get
            {
                return _length;
            }
            set
            {
                _length = value;
            }
        }



        public bool IsDownload
        {
            get
            {
                return Type == FileTaskType.Download;
            }
        }

        public string TcpAddressString
        {
            get
            {
                return TcpAddress.ToString();
            }
        }

        public string RemoteDirectory
        {
            get
            {
                return RemotePath.Substring(0, RemotePath.LastIndexOf("\\") + 1);
            }
        }

        public string Name
        {
            get
            {
                int idx = RemotePath.LastIndexOf("\\") + 1;
                return RemotePath.Substring(idx, RemotePath.Length - idx);
            }
        }

        


        public string Size
        {
            get
            {
                if (Length < 0) return "unknown";
                //if (IsDirectory) return "";
                if ((Length / (1 << 30)) > 0)
                {
                    double size = (double)(Length >> 20) / 1024;
                    return size.ToString("0.00") + " G";
                }
                else if ((Length / (1 << 20)) > 0)
                {
                    double size = (double)(Length >> 10) / 1024;
                    return size.ToString("0.00") + " M";
                }
                else if ((Length / (1 << 10)) > 0)
                {
                    double size = (double)Length / 1024;
                    return size.ToString("0.00") + " K";
                }
                else
                {
                    return Length.ToString() + " B";
                }
            }
            set
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Size"));
            }
        }

        private FileTaskStatus _status = FileTaskStatus.Waiting;

        public FileTaskStatus Status
        {
            get
            {
                return _status;
            }
            set
            {
                _status = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("StatusString"));
            }
        }

        public string StatusString
        {
            get
            {
                return _status.ToString();
            }
        }

        /// <summary>
        /// 向 sever 发送请求后取得 server 提供的 filestream id
        /// </summary>
        public int FileStreamId { get; set; } = -1;


        /// <summary>
        /// socket 包计数, 已传输完成的 package 数量
        /// </summary>
        public int FinishedPacket { get; set; } = 0;
        public int TotalPacket
        {
            get
            {
                return (int)(Length / HB32Encoding.DataSize) + (Length % HB32Encoding.DataSize > 0 ? 1 : 0);
            }
        }

        public override string ToString()
        {
            return string.Format("IsDir={0}, Type={1}, RemotePath={2}, LocalPath={3}, Length={4}, Size={5}",
                IsDirectory.ToString(), Type, RemotePath, LocalPath, Length, Size);
        }



        public event PropertyChangedEventHandler PropertyChanged;
    }
}
