using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketFileManager.Models
{
    public class FileViewModel
    {
        public string Name { get; set; }
        public string Size { get; set; }
        public bool IsDirectory { get; set; }
        /*
        public FileViewModel(string name, long length,bool isDirectory)
        {
            Name = name;

            if (isDirectory) { Size = ""; }
            if ((length & (1 << 30)) > 0)
            {
                double size = (double)(length >> 20) / 1024;
                return size.ToString("0.00") + " G";
            }
            else if ((Length & (1 << 20)) > 0)
            {
                double size = (double)(length >> 10) / 1024;
                return size.ToString("0.00") + " M";
            }
            else if ((Length & (1 << 10)) > 0)
            {
                double size = (double)length / 1024;
                return size.ToString("0.00") + " K";
            }
            else
            {
                return Size.ToString() + " B";
            }
        }
        */
    }
}
