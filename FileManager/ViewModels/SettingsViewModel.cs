using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

using FileManager.Static;

namespace FileManager.ViewModels
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;


        public bool ClickCloseToMinimize
        {
            get
            {
                return Config.ClickCloseToMinimize;
            }
            set
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ClickCloseToMinimize"));
            }
        }

        public bool ClickCloseToClose
        {
            get
            {
                return !Config.ClickCloseToMinimize;
            }
            set
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ClickCloseToClose"));
            }
        }

        public string UpdateLengthThreshold
        {
            get
            {
                return ConvertLengthToBytesString(Config.UpdateLengthThreshold);
            }
            set
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("UpdateLengthThreshold"));
            }
        }

        public string UpdateTimeThreshold
        {
            get
            {
                return Config.UpdateTimeThreshold.ToString();
            }
            set
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("UpdateTimeThreshold"));
            }
        }

        public string DefaultPort
        {
            get
            {
                return Config.DefaultServerPort.ToString();
            }
            set
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("DefaultPort"));
            }
        }


        public string SocketSendTimeout
        {
            get
            {
                return Config.SocketSendTimeout.ToString();
            }
            set
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SocketSendTimeout"));
            }
        }
        public string SocketReceiveTimeout
        {
            get
            {
                return Config.SocketReceiveTimeout.ToString();
            }
            set
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SocketReceiveTimeout"));
            }
        }

        public string SmallFileThreshold
        {
            get
            {
                return ConvertLengthToBytesString(Config.SmallFileThreshold);
            }
            set
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SmallFileThreshold"));
            }
        }

        public string ThreadLimit
        {
            get
            {
                return Config.ThreadLimit.ToString();
            }
            set
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ThreadLimit"));
            }
        }

        public string SaveRecordInterval
        {
            get
            {
                return Config.SaveRecordInterval.ToString();
            }
            set
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SaveRecordInterval"));
            }
        }

        public string ConnectionMonitorRecordCount
        {
            get
            {
                return Config.ConnectionMonitorRecordCount.ToString();
            }
            set
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ConnectionMonitorRecordCount"));
            }
        }

        public string ConnectionMonitorRecordInterval
        {
            get
            {
                return Config.ConnectionMonitorRecordInterval.ToString();
            }
            set
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ConnectionMonitorRecordInterval"));
            }
        }


        public static readonly string[] suffixes = new string[] { "T", "G", "M", "K", "" };

        public static string ConvertLengthToBytesString(long len)
        {
            
            long[] len_part = new long[suffixes.Length];
            for (int i = len_part.Length - 1; i >= 0; --i)
            {
                len_part[i] = len & ((1 << 10) - 1);
                len = len >> 10;
            }
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < suffixes.Length; ++i)
            {
                if (len_part[i] != 0)
                {
                    sb.Append(len_part[i]).Append(suffixes[i]);
                }
            }
            return sb.ToString();
        }

        public static bool TryParseBytesString(string str, out long len)
        {
            str = str.ToUpper();
            len = 0;
            if (Regex.IsMatch(str, @"^(\d*T)?(\d*G)?(\d*M)?(\d*K)?\d*$"))
            {
                long[] len_part = new long[suffixes.Length];
                string[] str_part = new string[suffixes.Length];
                str_part[0] = str;
                for (int i = 0; i < suffixes.Length - 1; ++i)
                {
                    if (str.Contains(suffixes[i]))
                    {
                        var a = str_part[i].Split(new string[] { suffixes[i] }, StringSplitOptions.None);
                        str_part[i] = a[0];
                        str_part[i + 1] = "0" + a[1];
                    }
                    else
                    {
                        str_part[i + 1] = str_part[i];
                        str_part[i] = "0";
                    }
                }
                try
                {
                    for (int i = 0; i < suffixes.Length; ++i)
                    {
                        len = (len << 10) + long.Parse(str_part[i]);
                    }
                }
                catch (Exception)
                {
                    return false;
                }
                return true;
            }
            return false;
        }

    }
}
