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
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.SendTimeout = SendTimeout;
            socket.ReceiveTimeout = ReceiveTimeout;
            /// BeginConnect为异步代码, 无法捕捉异常, 所以只能写成这种方式
            socket.BeginConnect(ipe, asyncResult =>
            {
                try
                {
                    socket.EndConnect(asyncResult);
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


        /// <summary>
        /// 向 Server 端发送 DisconnectRequest 后关闭 Socket 连接
        /// 即使不成功也不会抛出异常
        /// </summary>
        public override void Close()
        {
            try
            {
                //SendHeader(PacketType.DisconnectRequest);
                socket.Close();
            }
            catch { }
        }



    }

}
