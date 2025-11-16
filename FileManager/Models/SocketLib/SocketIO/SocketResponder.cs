using FileManager.Models.SocketLib.HbProtocol;
using FileManager.Models.SocketLib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.Models.SocketLib.SocketIO
{
    /// <summary>
    /// 通信服务端, 与 client/relay 通信
    /// /* 一般通过 servre.Accept() 获得的 Socket 对象初始化 */
    /// </summary>
    public class SocketResponder : SocketEndPoint
    {
        public SocketResponder(Socket socket)
        {
            this.socket = socket;
        }
    }
}
