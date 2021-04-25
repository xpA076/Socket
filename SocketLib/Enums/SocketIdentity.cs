using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketLib.Enums
{
    public enum SocketIdentity : int
    {
        None = 0x0,
        Guest = 0x1,
        Authenticated = 0x3,
        Admin = 0xF,
    }
}
