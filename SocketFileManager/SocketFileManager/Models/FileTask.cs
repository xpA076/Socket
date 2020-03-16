using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SocketFileManager.SocketLib;

namespace SocketFileManager.Models
{
    public class FileTask: INotifyPropertyChanged
    {
        public bool IsDirectory { get; set; } = false;
        public string Type { get; set; } = "download";
        public string RemotePath { get; set; }
        public string LocalPath { get; set; }
        public long Length { get; set; } = 0;
        private string status = "--";

        /// <summary>
        /// socket 包计数, 已申请过的最后一个包索引值
        /// </summary>
        public int LastPackage { get; set; } = -1;
        public int TotalPackage
        {
            get
            {
                return (int)(Length / HB32Encoding.DataSize) + (Length % HB32Encoding.DataSize > 0 ? 1 : 0);
            }
        }
        /// <summary>
        /// 向 sever 发送请求后取得 server 提供的 filestrem id
        /// </summary>
        public int ServerId { get; set; } = -1;

        public string Size
        {
            get
            {
                if (IsDirectory) return "";
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
        }

        
        public string Status
        {
            get
            {
                return status;
            }
            set
            {
                status = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Status"));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
