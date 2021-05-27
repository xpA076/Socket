using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.SocketLib
{
    public class HB32Encoding
    {
        public static readonly int HeaderSize = 32;
        public static readonly int DataSize = 4096;
        public static readonly int PacketSize = HeaderSize + DataSize;

    }
}
