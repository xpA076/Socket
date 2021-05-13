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

        public bool IsWithProxy { get; set; }


        private SocketLib.Enums.ProxyHeader NextProxyHeader { get; set; }


        private byte[] GetHeaderBytes(HB32Header header)
        {
            if (IsWithProxy)
            {
                byte[] bytes = new byte[34];
                bytes[0] = 0xA3;
                bytes[1] = (byte)NextProxyHeader;
                Array.Copy(header.GetBytes(), 0, bytes, 2, 32);
                return bytes;
            }
            else
            {
                return header.GetBytes();
            }
        }


        public void SendHeader(HB32Header header)
        {
            this.NextProxyHeader = SocketLib.Enums.ProxyHeader.SendBytes;
            SocketClient.SendHeader(header);
        }


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
