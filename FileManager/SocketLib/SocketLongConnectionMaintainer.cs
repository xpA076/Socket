using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FileManager.Events;
using FileManager.SocketLib.Enums;
using FileManager.SocketLib.SocketServer;

namespace FileManager.SocketLib
{
    public class SocketLongConnectionMaintainer : SocketIO
    {
        public string Name { get; set; }

        public bool IsKeepLongConnection { get; set; } = true;

        public bool IsAccepting { get; set; } = true;

        public int DefaultSendTimeout { get; set; } = 3000;

        public int DefaultReceiveTimeout { get; set; } = 3000;

        public int LongConnectionTimeout { get; set; } = 20 * 1000;


        private SocketClient LongConnectClient;

        /// <summary>
        /// Maintainer 一定会直接连接到一个ProxyServer上 (不再经过其它代理, 否则反向代理无意义)
        /// </summary>
        public TCPAddress ProxyServerAddres { get; set; }

        public SocketLongConnectionMaintainer(TCPAddress proxy_address, string name)
        {
            ProxyServerAddres = proxy_address.Copy();
            Name = name;
            //todo RouteNode
            LongConnectClient = new SocketClient(proxy_address);
        }



        public Socket Accept()
        {
            while (IsAccepting)
            {
                HB32Header header;
                byte[] bytes;
                try
                {
                    LongConnectClient.SendHeader(SocketPacketFlag.ReverserProxyQuery);
                    LongConnectClient.ReceiveBytes(out header, out bytes);
                }
                catch(Exception ex)
                {
                    Log("Long connection exception : " + ex.Message, LogLevel.Warn);
                    StartLongConnection();
                    continue;
                }
                try
                {
                    if (header.I1 == 1)
                    {
                        IPEndPoint ipe = new IPEndPoint(ProxyServerAddres.IP, ProxyServerAddres.Port);
                        Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        s.SendTimeout = DefaultSendTimeout;
                        s.ReceiveTimeout = DefaultReceiveTimeout;
                        s.Connect(ipe);
                        return s;
                    }
                }
                catch (Exception ex)
                {
                    Log("Reversed server Accept() exception : " + ex.Message, LogLevel.Error);
                }
            }
            return null;
        }


        public void StartLongConnection()
        {
            while (IsKeepLongConnection)
            {
                try
                {
                    // todo 可以考虑改
                    LongConnectClient.Connect(LongConnectionTimeout, LongConnectionTimeout);
                    //LongConnectClient.SendRawbytes(new byte[2] { SocketProxy.ReversedProxyHeaderByte, 0x00 });
                    LongConnectClient.SendBytes(SocketPacketFlag.ReversedProxyBuild, Name);
                    LongConnectClient.ReceiveBytes(out HB32Header header, out byte[] bytes);
                    if (header.Flag != SocketPacketFlag.ReversedProxyResponse)
                    {
                        throw new ArgumentException(Encoding.UTF8.GetString(bytes));
                    }
                    return;
                }
                catch (Exception ex)
                {
                    Log("Start long connection exception : " + ex.Message, LogLevel.Error);
                }
            }
        }

    }
}
