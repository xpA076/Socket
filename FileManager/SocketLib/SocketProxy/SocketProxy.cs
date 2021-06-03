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
        private class ReversedServerInfo
        {
            public EventWaitHandle WaitHandle;
        }


        public static readonly byte ProxyHeaderByte = 0xA3;
        //public static readonly byte ReversedProxyHeaderByte = 0xA4;

        public SocketProxyConfig Config { get; set; } = new SocketProxyConfig();

        public SocketProxy(IPAddress ip) : base(ip)
        {

        }

        private readonly Dictionary<string, ReversedServerInfo> ReversedProxyServers = new Dictionary<string, ReversedServerInfo>();


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


        protected override void ReceiveData(object responderObject)
        {
            SocketResponder responder = responderObject as SocketResponder;
            responder.SetTimeout(Config.SocketSendTimeout, Config.SocketReceiveTimeout);
            SocketSender sender = null;
            try
            {
                responder.ReceiveBytes(out HB32Header route_header, out byte[] route_bytes);
                if (route_header.Flag == SocketPacketFlag.ProxyRouteRequest)
                {
                    /// Forward connection proxy (from client-side)
                    sender = ForwardConnectionProxy(responder, route_header, route_bytes);
                    /// Communication proxy
                    
                }
                else if (route_header.Flag == SocketPacketFlag.ReversedProxyConnectionRequest)
                {
                    /// Reversed connection proxy (from server-side, triggered in server by long connection)
                    sender = ReversedConnectionProxy(ref responder, route_header, route_bytes);
                    /// Communication proxy

                }
                else if (route_header.Flag == SocketPacketFlag.ReversedProxyLongConnectionRequest)
                {
                    /// Reversed-server long connection socket


                }
                else { }
            }
            catch (Exception ex)
            {
                Log("Socket initiate exception :" + ex.Message, LogLevel.Error);
                //DisposeClient(sender, responder); 这行不能删
            }
        }


        /// <summary>
        /// 当上一级代理不是挂载在当前代理上的节点上时, 利用此方法完成同方向代理的中继, 并返回上级代理结果
        /// </summary>
        /// <param name="responder"></param>
        /// <param name="route"></param>
        /// <returns></returns>
        private SocketSender ConnectionRelay(SocketResponder responder, HB32Header header, ConnectionRoute route)
        {
            if (route.IsNextNodeProxy)
            {
                /// 继续正向代理
                SocketSender sender = new SocketSender(true);
                string err_msg = "";
                try
                {
                    sender.ConnectWithTimeout(route.NextNode.Address, Config.BuildConnectionTimeout);
                    sender.SetTimeout(Config.SocketSendTimeout, Config.SocketReceiveTimeout);
                }
                catch (Exception ex)
                {
                    /// 当前代理建立连接失败
                    err_msg = ex.Message;
                }
                if (string.IsNullOrEmpty(err_msg))
                {
                    HB32Header next_header = header.Copy();
                    next_header.I1++;
                    sender.SendBytes(next_header, route.GetBytes(node_start_index: 1));
                    sender.ReceiveBytes(out HB32Header respond_header, out byte[] respond_bytes);
                    if ((respond_header.Flag | SocketPacketFlag.ExceptionFlag) == 0)
                    {
                        responder.SendHeader(respond_header);
                    }
                    else
                    {
                        /// 上级或更上级代理建立连接失败, header 中包含抛出异常的代理位置
                        responder.SendBytes(respond_header, respond_bytes);
                    }
                }
                else
                {
                    HB32Header err_header = header.Copy();
                    err_header.Flag = (SocketPacketFlag)(((int)err_header.Flag & 0xFFFF00) | 0x90);
                    responder.SendBytes(err_header, err_msg);
                }
                return sender;
            }
            else
            {
                /// 直连 server
                SocketSender sender = new SocketSender(false);
                string err_msg = "";
                try
                {
                    sender.ConnectWithTimeout(route.ServerAddress.Address, Config.BuildConnectionTimeout);
                    sender.SetTimeout(Config.SocketSendTimeout, Config.SocketReceiveTimeout);
                }
                catch (Exception ex)
                {
                    err_msg = ex.Message;
                }
                /// response
                if (string.IsNullOrEmpty(err_msg))
                {
                    HB32Header resp_header = header.Copy();
                    resp_header.Flag = (SocketPacketFlag)(((int)resp_header.Flag & 0xFFFF00) | 0x10);
                    responder.SendHeader(resp_header);
                }
                else
                {
                    HB32Header err_header = header.Copy();
                    err_header.Flag = (SocketPacketFlag)(((int)err_header.Flag & 0xFFFF00) | 0x90);
                    responder.SendBytes(err_header, err_msg);
                }
                return sender;
            }
        }


        /// <summary>
        /// 创建完整代理隧道, 向下级发送建立连接是否成功信号, 返回与上级通信的 SocketSender 
        /// [上级可能是 Proxy, Server, Reversed Server, Reversed Server's Proxy, etc....]
        /// </summary>
        /// <param name="responder"></param>
        /// <param name="route_bytes"></param>
        /// <returns></returns>
        private SocketSender ForwardConnectionProxy(SocketResponder responder, HB32Header route_header, byte[] route_bytes)
        {
            ConnectionRoute route = ConnectionRoute.FromBytes(route_bytes);
            if (route.NextNode.Address.Equals(this.HostAddress))
            {
                /// 为连接到此的反向代理
                // todo
                // 可以试试 ReadWriteLock
                if (ReversedProxyServers.ContainsKey(route.NextNode.Name))
                {
                    ReversedServerInfo server = ReversedProxyServers[route.NextNode.Name];
                    /// Set EventWaitHandle
                    
                    /// Wait for new connection built from server
                }
                else
                {
                    //todo 没有对应挂载的 Server
                }




                return null;
            }
            else
            {
                return ConnectionRelay(responder, route_header, route);
            }
        }


        private SocketSender ReversedConnectionProxy(ref SocketResponder responder, HB32Header route_header, byte[] route_bytes)
        {
            ConnectionRoute route = ConnectionRoute.FromBytes(route_bytes);
            if (route.NextNode.Address.Equals(this.HostAddress))
            {
                if (route.IsNextNodeProxy)
                {
                    // todo: 反向代理

                }
                else
                {

                }


                return null;
            }
            else
            {
                SocketSender sender = ConnectionRelay(responder, route_header, route);
                SocketResponder r = sender.ConvertToResponder();
                SocketSender s = responder.ConvertToSender(route_header.I1 > 0);
                responder = r;
                return s;
            }
        }


        private void LongConnectionRespond(SocketResponder responder, HB32Header route_header, byte[] route_bytes)
        {
            /// LongConnection 中继, 或 将Reversed Server 挂载在此
        }



        /*

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
        */



        /// <summary>
        /// 完成创建 Socket 过程身份认证的代理过程
        /// </summary>
        /// <param name="client"></param>
        /// <returns>已与Server或下级代理连接成功的 SocketEndPoint 对象</returns>
        private SocketSender AuthenticationProxy(Socket client)
        {
            // todo 重写 21.05.28
            byte[] proxy_header;
            SocketIO.ReceiveBytes(client, out HB32Header route_header, out byte[] route_bytes);
            Debug.Assert(route_bytes[0] == 1);
            int pt = 0;
            ConnectionRoute route = ConnectionRoute.FromBytes(route_bytes, ref pt);
            byte[] key_bytes = new byte[route_bytes.Length - pt];
            Array.Copy(route_bytes, pt, key_bytes, 0, key_bytes.Length);
            SocketSender proxy_client = new SocketSender(null, route.IsNextNodeProxy);
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
                proxy_client.Connect(route.NextNode.Address, Config.SocketSendTimeout, Config.SocketReceiveTimeout);
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


        private void DisposeClient(SocketSender sender, SocketResponder responder)
        {
            try
            {
                sender.Close();
            }
            catch (Exception) { }
            try
            {
                responder.Close();
            }
            catch (Exception) { }
        }
    }
}
