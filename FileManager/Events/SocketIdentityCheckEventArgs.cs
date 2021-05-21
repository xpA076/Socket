using FileManager.SocketLib;
using FileManager.SocketLib.Enums;
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
        public SocketIdentity CheckedIndentity { get; set; } = SocketIdentity.None;

        public readonly HB32Header header;

        public readonly byte[] bytes;

        public SocketIdentityCheckEventArgs(HB32Header header, byte[] bytes)
        {
            this.header = header;
            this.bytes = bytes;
        }
    }
}
