using FileManager.SocketLib.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.SocketLib
{
    /// <summary>
    /// 通信服务端, 与 client/relay 通信
    /// /* 一般通过 servre.Accept() 获得的 Socket 对象初始化 */
    /// </summary>
    public class SocketResponder : SocketEndPoint
    {
        public SocketResponder()
        {
            this.IsRequireProxyHeader = false;
        }


        public SocketResponder(Socket socket)
        {
            this.socket = socket;
            this.IsRequireProxyHeader = false;
        }


        public SocketSender ConvertToSender(bool isWithProxy)
        {
            SocketSender sender = new SocketSender(this.socket, isWithProxy);
            return sender;
        }


        /// <summary>
        /// 在 SocketProxy 中调用, 对于socket 在 ReceiveProxyHeader 后调用
        /// </summary>
        /// <param name="header"></param>
        /// <param name="bytes"></param>
        public void ReceiveBytesWithoutProxyHeader(out HB32Header header, out byte[] bytes)
        {
            SocketIO.ReceiveBytes(socket, out header, out bytes);
        }

    }
}
