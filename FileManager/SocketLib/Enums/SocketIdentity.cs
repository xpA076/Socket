using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.SocketLib.Enums
{
    public delegate SocketIdentity SocketIdentityCheckHandler(HB32Header header, byte[] bytes);

    public enum SocketIdentity : int
    {
        None = 0x0,
        ReadFile = 0x1,
        WriteFile = 0x2,
        RemoteRun = 0x8,
        All = 0xF
    }


}
