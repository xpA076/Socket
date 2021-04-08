using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace ServerConsoleApp
{
    public class ServerFileInfoClass
    {
        public FileStream FStream { get; set; }

        public string ServerPath { get; set; }

        public long Length { get; set; }

        public DateTime LastTime { get; set; }
    }
}
