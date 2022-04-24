using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.ViewModels.PageTransfer
{
    public class ListViewTransferItem : IComparable
    {
        public int TaskIndex { get; set; }

        public bool IsDownload { get; set; } = false;

        public bool IsDirectory { get; set; } = false;

        public string RemoteDirectory { get; set; } = "";

        public string LocalDirectory { get; set; } = "";

        public string Name { get; set; } = "";

        public string Status { get; set; } = "";

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
