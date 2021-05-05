using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SocketLib;

namespace FileManager.Models
{
    internal class Communicator
    {
        private SocketLib.SocketClient SocketClient { get; set; }

        public bool IsUseProxy { get; set; }

        public void SendBytes(HB32Header header, byte[] bytes)
        {
            // todo
        }

        public void RecieveBytes(out HB32Header header, out byte[] bytes)
        {
            // todo
            header = null;
            bytes = null;
        }
    }
}
