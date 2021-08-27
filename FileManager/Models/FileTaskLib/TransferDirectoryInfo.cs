using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.Models
{
    public class TransferDirectoryInfo : TransferInfo
    {
        public bool IsComplete
        {
            // todo
            get; private set;
        }

        

        public List<TransferDirectoryInfo> DirectoryChildren { get; set; } = new List<TransferDirectoryInfo>();

        public List<TransferFileInfo> FileChildren { get; set; } = new List<TransferFileInfo>();

    }
}
