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

namespace FileManager.SocketLib.SocketProxy
{
    public class SocketProxy : SocketServerBase
    {
        public static readonly byte ProxyHeaderByte = 0xA3;

        public SocketProxyConfig Config { get; set; } = new SocketProxyConfig();

        public SocketProxy(IPAddress ip) : base(ip)
        {

        }

        public override void ReceiveData(object acceptSocketObject)
        {
            Socket client = (Socket)acceptSocketObject;
            SocketEndPoint socket_ep = null;
            try
            {
                client.SendTimeout = Config.SocketSendTimeOut;
                client.ReceiveTimeout = Config.SocketReceiveTimeOut;
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
                                this.ReceiveBytes(client, out recv_header, out recv_bytes);
                                socket_ep.SendHeader(recv_header);
                                break;
                            case ProxyHeader.SendBytes:
                                this.ReceiveBytes(client, out recv_header, out recv_bytes);
                                socket_ep.SendBytes(recv_header, recv_bytes);
                                break;
                            case ProxyHeader.ReceiveBytes:
                                socket_ep.ReceiveBytes(out recv_header, out recv_bytes);
                                this.SendBytes(client, recv_header, recv_bytes);
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
                                return;
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
                            return;
                        }
                        if (ex.Message.Contains("Invalid socket header"))
                        {
                            DisposeClient(client, socket_ep);
                            Log("Connection closed : " + ex.Message, LogLevel.Warn);
                            return;
                        }
                        Log("Server exception :" + ex.Message, LogLevel.Warn);
                        Thread.Sleep(200);
                        continue;
                    }
                }
                Log("Connection closed.", LogLevel.Warn);
            }
            catch (Exception ex)
            {
                Log("Socket initiate exception :" + ex.Message, LogLevel.Error);
                DisposeClient(client, socket_ep);
            }
        }


        /// <summary>
        /// Receive proxy 包头标志
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        private byte[] ReceiveProxyHeader(Socket client)
        {
            byte[] bytes = new byte[2];
            this.ReceiveBuffer(client, bytes, 2);
            return bytes;
        }


        /// <summary>
        /// 完成创建 Socket 过程身份认证的代理过程
        /// </summary>
        /// <param name="client"></param>
        /// <returns>已与Server或下级代理连接成功的 SocketEndPoint 对象</returns>
        private SocketEndPoint AuthenticationProxy(Socket client)
        {
            byte[] proxy_header = ReceiveProxyHeader(client);
            this.ReceiveBytes(client, out HB32Header route_header, out byte[] route_bytes);
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
        }


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
                socket_ep.Close();
            }
            catch (Exception) { }
        }
    }
}
