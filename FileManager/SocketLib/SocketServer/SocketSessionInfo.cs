using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FileManager.SocketLib.Enums;

namespace FileManager.SocketLib.SocketServer
{

    /// <summary>
    /// SocketSession, 调用方负责线程安全
    /// </summary>
    public class SocketSessionInfo
    {
        public SocketIdentity Identity = SocketIdentity.None;


    }
}
