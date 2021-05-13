﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketLib.Enums
{
    public enum ProxyHeader : byte
    {
        None = 0,
        SendHeader,
        SendBytes,
        ReceiveHeader,
        ReceiveBytes,
    }
}
