using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.Models.SocketLib.HbProtocol
{
    public class HB32Response
    {
        public HB32Header Header { get; set; }
        public byte[] Bytes { get; set; }

        public HB32Response(HB32Header header, byte[] bytes)
        {
            Header = header;
            Bytes = bytes;
        }
    }
}
