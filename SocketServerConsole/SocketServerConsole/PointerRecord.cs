using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace SocketServerConsole
{
    public class PointerRecord
    {
        public FileStream Pointer { get; set; }

        public string ServerPath { get; set; }

        public long Length { get; set; }
    }
}
