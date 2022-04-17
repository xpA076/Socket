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
    public delegate void SocketAsyncCallbackEventHandler(object sender, EventArgs e);
    public delegate void SocketAsyncExceptionEventHandler(object sender, SocketAsyncExceptionEventArgs e);

    public class SocketAsyncExceptionEventArgs : EventArgs
    {
        public string ExceptionMessage => ThrowedException.Message;

        public Exception ThrowedException { get; set; }

        public SocketAsyncExceptionEventArgs(Exception ex)
        {
            ThrowedException = ex;
        }
    }




    public class SocketClient : SocketEndPoint
    {
        public event SocketAsyncCallbackEventHandler SocketAsyncCallback;
        public event SocketAsyncExceptionEventHandler SocketAsyncException;


        /// <summary>
        /// 目标Server (或直接连接的第一层Proxy) 地址
        /// 从 SocketFactory 中建立时 HostAddress 一定为非代理服务器, 即 Name == ""
        /// </summary>
        public RouteNode HostAddress { get; set; } = null;


        public SocketClient(RouteNode node_address, bool is_client_with_proxy)
        {
            HostAddress = node_address.Copy();
            IsRequireProxyHeader = is_client_with_proxy;
        }


        /// <summary>
        /// client 异步 connect, 连接成功后执行回调函数句柄
        /// </summary>
        /// <param name="asyncCallback"></param>
        public void AsyncConnect(int SendTimeout, int ReceiveTimeout)
        {
            IPEndPoint ipe = new IPEndPoint(HostAddress.Address.IP, HostAddress.Address.Port);
            client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            client.SendTimeout = SendTimeout;
            client.ReceiveTimeout = ReceiveTimeout;
            /// BeginConnect为异步代码, 无法捕捉异常, 所以只能写成这种方式
            client.BeginConnect(ipe, asyncResult =>
            {
                try
                {
                    client.EndConnect(asyncResult);
                    SocketAsyncCallback?.Invoke(this, EventArgs.Empty);
                }
                catch (Exception ex)
                {
                    SocketAsyncException?.Invoke(this, new SocketAsyncExceptionEventArgs(ex));
                }
            }, null);
        }


        public void Connect(int SendTimeout, int ReceiveTimeout)
        {
            Connect(HostAddress.Address, SendTimeout, ReceiveTimeout);
        }


        public void ConnectWithTimeout(int timeout)
        {
            ConnectWithTimeout(HostAddress.Address, timeout);
        }


        public override void Close()
        {
            try
            {
                SendHeader(HB32Packet.DisconnectRequest);
                client.Close();
            }
            catch { }
        }



    }

}
