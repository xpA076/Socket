using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.ViewModels.PageTransfer
{
    public class TransferListViewTiem
    {
        public bool IsDownload { get; set; } = false;

        public bool IsDirectory { get; set; } = false;

        public string RemoteDirectory { get; set; } = "";

        public string LocalDirectory { get; set; } = "";

        public string Name { get; set; } = "";


    }
}
