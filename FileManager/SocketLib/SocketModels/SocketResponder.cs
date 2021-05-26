using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.SocketLib
{
    /// <summary>
    /// 代理通信被动端, 与下级通信
    /// 一般通过 servre.Accept() 获得的 Socket 对象初始化
    /// </summary>
    public class SocketResponder : SocketEndPoint
    {
        public SocketResponder(Socket socket)
        {
            this.client = socket;
            this.IsRequireProxyHeader = false;
        }
    }
}
