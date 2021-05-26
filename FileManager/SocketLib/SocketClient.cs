using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

using FileManager.SocketLib.Enums;
using FileManager.SocketLib.Models;

namespace FileManager.SocketLib
{
    public class SocketClient : SocketEndPoint
    {

        /// <summary>
        /// 目标Server (或直接连接的第一层Proxy) 地址
        /// 从 SocketFactory 中建立时 HostAddress 一定为非代理服务器, 即 Name == ""
        /// </summary>
        public RouteNode HostAddress { get; set; } = null;


        public SocketClient(RouteNode node_address)
        {
            HostAddress = node_address.Copy();
        }


        /// <summary>
        /// client 异步 connect, 连接成功后执行回调函数句柄
        /// </summary>
        /// <param name="asyncCallback"></param>
        public void AsyncConnect(SocketAsyncCallback asyncCallback, SocketAsyncExceptionCallback exceptionCallback, int SendTimeout, int ReceiveTimeout)
        {
            IPEndPoint ipe = new IPEndPoint(HostAddress.Address.IP, HostAddress.Address.Port);
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
            Connect(HostAddress.Address, SendTimeout, ReceiveTimeout);
        }

        public override void Close()
        {
            try
            {
                SendHeader(SocketPacketFlag.DisconnectRequest);
                client.Close();
            }
            catch { }
        }



    }

}
