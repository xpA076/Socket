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
        public int TaskIndex { get; set; }
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
                return Name;
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
            throw new NotImplementedException();
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            throw new NotImplementedException();
        }
    }
}
