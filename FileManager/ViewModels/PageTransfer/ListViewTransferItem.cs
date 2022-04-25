using FileManager.Models.TransferLib.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.ViewModels.PageTransfer
{
    public class ListViewTransferItem : IComparable, INotifyPropertyChanged
    {
        public int TaskIndex { get; set; } = -1;

        public int Level { get; set; } = 0;

        public bool IsDownload { get; set; } = false;

        public bool IsDirectory { get; set; } = false;

        //public string RemoteDirectory { get; set; } = "";

        //public string LocalDirectory { get; set; } = "";

        public string RelativePath { get; set; }

        private string _name = "";

        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ViewName"));
            }
        }

        public string ViewName
        {
            get
            {
                /// todo 显示层级
                string s = "";
                for (int i = 0; i < Level - 1; ++i)
                {
                    s += "   ";
                }
                if (Level > 0)
                {
                    s += "-> ";
                }
                return s + Name;
            }
        }


        private long _size = 0;

        public long Size
        {
            get
            {
                return _size;
            }
            set
            {
                _size = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SizeString"));
            }
        }


        public string SizeString
        {
            get
            {
                return PageTransferViewModel.SizeToString(Size);
            }
        }


        private TransferStatus _status;

        public event PropertyChangedEventHandler PropertyChanged;

        public TransferStatus Status
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
                return Status.ToString();
            }
        }


        public int CompareTo(object obj)
        {
            ListViewTransferItem item = obj as ListViewTransferItem;
            /// 是否为同一个传输任务
            if (this.TaskIndex != item.TaskIndex)
            {
                return this.TaskIndex.CompareTo(item.TaskIndex);
            }
            /// 是否有根节点
            if (this.Level == 0)
            {
                if (item.Level == 0)
                {
                    return 0;
                }
                else
                {
                    return -1;
                }

            }
            else if (item.Level == 0)
            {
                return 1;
            }
            /// 均无根节点
            string[] split1 = this.RelativePath.Split('\\');
            string[] split2 = item.RelativePath.Split('\\');
            for (int i = 0; i < Math.Max(split1.Length, split2.Length); ++i)
            {
                /// 判断索引是否越界
                if (i >= split1.Length)
                {
                    /// 说明 split2 为 split1 的子目录
                    return -1;
                }
                else if (i >= split2.Length)
                {
                    /// 说明 split1 为 split2 的子目录
                    return 1;
                }
                /// 按子目录级别递进比较
                if (split1[i] != split2[i])
                {
                    return split1[i].CompareTo(split2[i]);
                }
                /// 当前级别相同, 进入下一级
            }
            /// 所有级别均相同, 二者相等
            return 0;
        }


        public override bool Equals(object obj)
        {
            ListViewTransferItem item = obj as ListViewTransferItem;
            if (item == null) return false;
            return this.TaskIndex == item.TaskIndex &&
                this.RelativePath == item.RelativePath &&
                this.IsDirectory == item.IsDirectory;
        }


        public override int GetHashCode()
        {
            return (TaskIndex + RelativePath).GetHashCode();
        }
    }
}
