using FileManager.Events;
using FileManager.Models.SocketLib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;


namespace FileManager.Models.SocketLib.SocketIO
{
    public delegate void SocketAsyncCallbackEventHandler(object sender, EventArgs e);
    public delegate void SocketAsyncExceptionEventHandler(object sender, SocketAsyncExceptionEventArgs e);

    public class SocketClient : SocketEndPoint
    {


        public SocketClient(TCPAddress address)
        {
        }


        /// <summary>
        /// 目标Server (或直接连接的第一层Proxy) 地址
        /// 从 SocketFactory 中建立时 HostAddress 一定为非代理服务器, 即 Name == ""
        /// </summary>
        public RouteNode HostAddress { get; set; } = null;




        public void ConnectWithTimeout(int timeout)
        {
            ConnectWithTimeout(timeout);
        }



    }

}
