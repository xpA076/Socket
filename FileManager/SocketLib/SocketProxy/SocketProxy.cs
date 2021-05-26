using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

using FileManager.SocketLib.SocketServer;
using FileManager.SocketLib.Enums;
using System.Threading;
using System.Diagnostics;

namespace FileManager.SocketLib
{
    public class SocketProxy : SocketServerBase
    {
        public static readonly byte ProxyHeaderByte = 0xA3;
        public static readonly byte ReversedProxyHeaderByte = 0xA4;

        public SocketProxyConfig Config { get; set; } = new SocketProxyConfig();

        public SocketProxy(IPAddress ip) : base(ip)
        {

        }

        private Dictionary<string, EventWaitHandle> ReversedProxyServers = new Dictionary<string, EventWaitHandle>();


        /// <summary>
        /// Receive proxy 包头标志
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        private byte[] ReceiveProxyHeader(Socket client)
        {
            byte[] bytes = new byte[2];
            SocketIO.ReceiveBuffer(client, bytes, 2);
            return bytes;
        }


        protected override void ReceiveData(object acceptSocketObject)
        {
            Socket client = (Socket)acceptSocketObject;
            SocketEndPoint socket_ep = null;
            try
            {
                client.SendTimeout = Config.SocketSendTimeOut;
                client.ReceiveTimeout = Config.SocketReceiveTimeOut;
                byte[] recv_1st_header = ReceiveProxyHeader(client);
                if (recv_1st_header[0] == ProxyHeaderByte)
                {
                    bool result = DoForwardProxy(client, socket_ep);
                    if (!result)
                    {
                        Log("Connection closed.", LogLevel.Warn);
                    }
                }
                else
                {
                    /// Not proxy header. Receive bytes:
                    ///   //(?)1. client 端请求与挂在该代理上的反向代理 Server 连接
                    ///   2. 反向代理Server向该代理的长连接轮询
                    /* 底层 Send / Receive 协议改了, 这段重写
                    byte[] header_bytes = new byte[HB32Encoding.HeaderSize];
                    Array.Copy(recv_1st_header, header_bytes, recv_1st_header.Length);
                    SocketIO.ReceiveBuffer(client, header_bytes, offset: recv_1st_header.Length);
                    SocketIO.ReceiveBytes(client, out HB32Header header, out byte[] bytes, HB32Header.ReadFromBytes(header_bytes));
                    /// 
                    string name = Encoding.UTF8.GetString(bytes);
                    SocketIO.SendHeader(client, SocketPacketFlag.ReversedProxyResponse);
                    client.SendTimeout = 20 * 1000;
                    client.ReceiveTimeout = 20 * 1000;
                    // todo : long connection
                    */
                }



                
            }
            catch (Exception ex)
            {
                Log("Socket initiate exception :" + ex.Message, LogLevel.Error);
                DisposeClient(client, socket_ep);
            }
        }


        private bool DoForwardProxy(Socket client, SocketEndPoint socket_ep)
        {
            socket_ep = AuthenticationProxy(client);
            int error_count = 0;
            while (error_count < 5)
            {
                try
                {
                    byte[] proxy_header = ReceiveProxyHeader(client);
                    HB32Header recv_header;
                    byte[] recv_bytes;
                    /*
                    switch ((ProxyHeader)proxy_header[1])
                    {
                        case ProxyHeader.SendHeader:
                            SocketIO.ReceiveBytes(client, out recv_header, out recv_bytes);
                            socket_ep.SendHeader(recv_header);
                            if (recv_header.Flag == SocketPacketFlag.DisconnectRequest)
                            {
                                DisposeClient(client, socket_ep);
                                return true;
                            }
                            break;
                        case ProxyHeader.SendBytes:
                            SocketIO.ReceiveBytes(client, out recv_header, out recv_bytes);
                            socket_ep.SendBytes(recv_header, recv_bytes);
                            break;
                        case ProxyHeader.ReceiveBytes:
                            socket_ep.ReceiveBytes(out recv_header, out recv_bytes);
                            SocketIO.SendBytes(client, recv_header, recv_bytes);
                            break;
                    }
                    */
                    error_count = 0;
                }
                catch (SocketException ex)
                {
                    error_count++;
                    switch (ex.ErrorCode)
                    {
                        // 远程 client 主机关闭连接
                        case 10054:
                            DisposeClient(client, socket_ep);
                            Log("Connection closed (client closed). " + ex.Message, LogLevel.Info);
                            return false;
                        // Socket 超时
                        case 10060:
                            Thread.Sleep(200);
                            Log("Socket timeout. " + ex.Message, LogLevel.Trace);
                            continue;
                        default:
                            Log("Server receive data :" + ex.Message, LogLevel.Warn);
                            continue;
                    }
                }
                catch (Exception ex)
                {
                    error_count++;
                    if (ex.Message.Contains("Buffer receive error: cannot receive package"))
                    {
                        DisposeClient(client, socket_ep);
                        Log(ex.Message, LogLevel.Trace);
                        return false;
                    }
                    if (ex.Message.Contains("Invalid socket header"))
                    {
                        DisposeClient(client, socket_ep);
                        Log("Connection closed : " + ex.Message, LogLevel.Warn);
                        return false;
                    }
                    Log("Server exception :" + ex.Message, LogLevel.Warn);
                    Thread.Sleep(200);
                    continue;
                }
            }
            return false;
        }




        /// <summary>
        /// 完成创建 Socket 过程身份认证的代理过程
        /// </summary>
        /// <param name="client"></param>
        /// <returns>已与Server或下级代理连接成功的 SocketEndPoint 对象</returns>
        private SocketSender AuthenticationProxy(Socket client)
        {
            byte[] proxy_header;
            SocketIO.ReceiveBytes(client, out HB32Header route_header, out byte[] route_bytes);
            Debug.Assert(route_bytes[0] == 1);
            int pt = 0;
            ConnectionRoute route = ConnectionRoute.FromBytes(route_bytes, ref pt);
            byte[] key_bytes = new byte[route_bytes.Length - pt];
            Array.Copy(route_bytes, pt, key_bytes, 0, key_bytes.Length);
            SocketSender proxy_client = new SocketSender(null, !route.IsNextNodeServer);
            byte[] bytes_to_send = new byte[0]; // todo
            // todo 异常处理
            if (route.NextNode.Address.Equals(this.HostAddress))
            {
                /// 下级为挂载在此的反向代理
                /// 利用与反向代理的长连接新建 socket 并通信
            }
            else
            {
                /// 下级需正向代理
                proxy_client.Connect(route.NextNode.Address, Config.SocketSendTimeOut, Config.SocketReceiveTimeOut);
            }
            proxy_client.SendBytes(route_header, bytes_to_send);
            if (proxy_client.IsRequireProxyHeader)
            {
                ReceiveProxyHeader(client);
            }
            proxy_client.ReceiveBytes(out HB32Header auth_header, out byte[] auth_bytes);
            //SocketIO.SendBytes(client, auth_header, auth_bytes);
            return proxy_client;








            /*


            GetAimInfo(route_bytes, out TCPAddress AimAddress, out bool IsAimProxy, out byte[] AimBytes);
            SocketClient socket_client = new SocketClient(AimAddress);
            socket_client.IsWithProxy = IsAimProxy;
            try
            {
                socket_client.Connect(Config.SocketSendTimeOut, Config.SocketReceiveTimeOut);
                socket_client.SendBytes(route_header, AimBytes);
            }
            catch(Exception ex)
            {
                proxy_header = ReceiveProxyHeader(client);
                this.SendBytes(client, SocketPacketFlag.AuthenticationException, "Proxy connect to server failed.");
                throw new Exception("Cannot connect to server : " + ex.Message);
            }
            proxy_header = ReceiveProxyHeader(client);
            socket_client.ReceiveBytes(out HB32Header auth_header, out byte[] auth_bytes);
            this.SendBytes(client, auth_header, auth_bytes);
            return socket_client;
            */
        }


        /// <summary>
        /// 从收到的 route_bytes 中获取需与下级服务器通信相关内容
        /// 本方法不产生 socket 通信
        /// </summary>
        /// <param name="recv_bytes">接收到来自client的路由数据</param>
        /// <param name="address">返回代理下级服务器地址</param>
        /// <param name="is_aim_proxy">代理下级服务器是否为代理 (决定socket_ep包头)</param>
        /// <param name="bytes_to_send">需向下级代理发送的内容</param>
        private void GetAimInfo(byte[] recv_bytes, out TCPAddress address, out bool is_aim_proxy, out byte[] bytes_to_send)
        {
            if (recv_bytes[0] == 1)
            {
                byte count = recv_bytes[1];
                if (count == 0)
                {
                    address = TCPAddress.FromBytes(recv_bytes, 2);
                    is_aim_proxy = false;
                    bytes_to_send = new byte[recv_bytes.Length - 8];
                    Array.Copy(recv_bytes, 8, bytes_to_send, 0, bytes_to_send.Length);
                }
                else
                {
                    address = TCPAddress.FromBytes(recv_bytes, 8);
                    is_aim_proxy = true;
                    bytes_to_send = new byte[recv_bytes.Length - 6];
                    Array.Copy(recv_bytes, bytes_to_send, 8);
                    Array.Copy(recv_bytes, 14, bytes_to_send, 8, bytes_to_send.Length - 8);
                    bytes_to_send[1] = (byte)(count - 1);
                }
                return;
            }
            address = new TCPAddress();
            is_aim_proxy = false;
            bytes_to_send = new byte[0];
        }


        private void DisposeClient(Socket client, SocketEndPoint socket_ep)
        {
            try
            {
                client.Close();
            }
            catch (Exception) { }
            try
            {
                socket_ep.CloseSocket();
            }
            catch (Exception) { }
        }
    }
}
