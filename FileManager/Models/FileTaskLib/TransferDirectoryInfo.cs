using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.Models
{
    public class TransferDirectoryInfo : TransferInfo
    {



        public int QueryCompleteCount { get; set; } = 0;

        public int TransferCompleteCount { get; set; } = 0;


        public List<TransferDirectoryInfo> DirectoryChildren { get; set; } = new List<TransferDirectoryInfo>();

        public List<TransferFileInfo> FileChildren { get; set; } = new List<TransferFileInfo>();


        private int _bytes_length = 0;

        public int BytesLength
        {
            get
            {
                if (_bytes_length == 0)
                {
                    byte[] bs_name = Encoding.UTF8.GetBytes(Name);
                    _bytes_length = 4 + bs_name.Length + 8 + 8 + 8 + 8 + 4;
                }
                return _bytes_length;
            }
        }



        public bool IsRoot()
        {
            return this.Parent == null;
        }


        public void SaveToFile(FileStream fs)
        {

        }

    }
}
