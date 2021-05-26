using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.SocketLib
{
    /// <summary>
    /// 代理通信主动端, 与上级通信
    /// </summary>
    public class SocketSender : SocketEndPoint
    {
        public SocketSender(Socket socket, bool isWithProxy)
        {
            this.client = socket;
            this.IsRequireProxyHeader = isWithProxy;
        }
    }
}
