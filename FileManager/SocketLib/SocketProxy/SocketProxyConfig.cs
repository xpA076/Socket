using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.SocketLib
{
    public class SocketProxyConfig
    {
        public int ProxyPort { get; set; } = 12139;

        public int SocketSendTimeout { get; set; } = 10000;

        public int SocketReceiveTimeout { get; set; } = 10000;

        public int BuildConnectionTimeout { get; set; } = 2000;
    }
}
