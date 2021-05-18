using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

using FileManager.SocketLib.Enums;

namespace FileManager.SocketLib
{
    public class SocketClient : SocketEndPoint
    {

        /// <summary>
        /// 目标Server (或直接连接的第一层Proxy) 地址
        /// </summary>
        public TCPAddress HostAddress { get; set; } = null;


        public SocketClient(TCPAddress tcpAddress)
        {
            HostAddress = tcpAddress.Copy();
            this.GetHeaderBytesFunc = this.GetHeaderBytesWithProxy;
        }

        


        /// <summary>
        /// client 异步 connect, 连接成功后执行回调函数句柄
        /// </summary>
        /// <param name="asyncCallback"></param>
        public void AsyncConnect(SocketAsyncCallback asyncCallback, SocketAsyncExceptionCallback exceptionCallback, int SendTimeout, int ReceiveTimeout)
        {
            IPEndPoint ipe = new IPEndPoint(HostAddress.IP, HostAddress.Port);
            client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            client.SendTimeout = SendTimeout;
            client.ReceiveTimeout = ReceiveTimeout;
            /// BeginConnect为异步代码, 无法捕捉异常, 所以只能写成这种方式
            client.BeginConnect(ipe, asyncResult => {
                try
                {
                    client.EndConnect(asyncResult);
                    asyncCallback();
                }
                catch(Exception ex)
                {
                    exceptionCallback(ex);
                }
            }, null);
        }

        public void Connect(int SendTimeout, int ReceiveTimeout)
        {
            IPEndPoint ipe = new IPEndPoint(HostAddress.IP, HostAddress.Port);
            client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            client.SendTimeout = SendTimeout;
            client.ReceiveTimeout = ReceiveTimeout;
            client.Connect(ipe);
            //client.Blocking = true;
        }




    }

}
