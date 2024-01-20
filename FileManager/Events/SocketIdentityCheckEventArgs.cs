using FileManager.Models.SocketLib.Enums;
using FileManager.Models.SocketLib.HbProtocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.Events
{
    public delegate void SocketIdentityCheckEventHandler(object sender, SocketIdentityCheckEventArgs e);

    public class SocketIdentityCheckEventArgs : EventArgs
    {
        public SocketIdentity Identity;

        public readonly HB32Header Header;

        public readonly byte[] KeyBytes;

        public SocketIdentityCheckEventArgs(byte[] key_bytes)
        {
            this.KeyBytes = key_bytes;
        }
    }
}
