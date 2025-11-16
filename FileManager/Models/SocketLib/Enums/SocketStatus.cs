using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.Models.SocketLib.Enums
{
    public enum SocketStatus : uint
    {
        Connected = 0,
        ZeroReceive = 0x1,
        ZeroSend = 0x2,
        WrongMagicHeader = 0x3,
        WrongCheckSum = 0x4,
        EncryptExcepton = 0x5,
        DecryptExcepton = 0x6,
        ConnectionTimeout = 0x7,
    }
}
