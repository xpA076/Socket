using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketFileManager.SocketLib
{
    public class FileClass
    {
        public string Name { get; set; }
        public long Length { get; set; } = 0;
        public bool IsDirectory { get; set; } = false;

        public static int Compare(FileClass f1, FileClass f2)
        {
            if (f1.IsDirectory == f2.IsDirectory)
            {
                return f1.Name.CompareTo(f2.Name);
            }
            else
            {
                return f1.IsDirectory ? -1 : 1;
            }
        }
    }
}
