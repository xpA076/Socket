using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.SocketLib.Enums
{
    public enum SocketIdentity : int
    {
        None = 0x0,
        Query = 0x1,
        ReadFile = 0x2,
        WriteFile = 0x4,
        RemoteRun = 0x8,
        All = 0xF
    }
}
