using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.SocketLib.SocketProxy
{
    public class SocketProxyConfig
    {
        public int ProxyPort { get; set; } = 12139;

        public int SocketSendTimeOut { get; set; } = 10000;

        public int SocketReceiveTimeOut { get; set; } = 10000;


    }
}
