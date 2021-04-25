using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

using SocketLib.Enums;

namespace SocketLib
{
    public class SocketClient : SocketIO
    {
        public Socket client = null;

        private IPAddress Hostip;
        private int Port;

        public static int SendTimeout { get; set; } = 3000;
        public static int ReceiveTimeout { get; set; } = 3000;

        private SocketAsyncExceptionCallback asyncExceptionCallback = null;

        public SocketClient(string ip, string port, SocketAsyncExceptionCallback c = null)
            : this(IPAddress.Parse(ip), int.Parse(port), c)
        {
            ;
        }

        public SocketClient(string ip, int port, SocketAsyncExceptionCallback c = null) 
            : this(IPAddress.Parse(ip), port, c)
        {

        }

        public SocketClient(TCPAddress tcpAddress, SocketAsyncExceptionCallback c = null) 
            : this(tcpAddress.IP, tcpAddress.Port, c)
        {

        }

        public SocketClient(IPAddress ip, int port, SocketAsyncExceptionCallback c = null)
        {
            Hostip = ip;
            Port = port;
            asyncExceptionCallback = c;
        }

        public void SendBytes(SocketPacketFlag flag, byte[] bytes, int i1 = 0, int i2 = 0, int i3 = 0)
        {
            SendBytes(client, flag, bytes, i1, i2, i3);
        }

        public void SendBytes(SocketPacketFlag flag, string str, int i1 = 0, int i2 = 0, int i3 = 0)
        {
            SendBytes(flag, Encoding.UTF8.GetBytes(str), i1, i2, i3);
        }

        /// <summary>
        /// 获取 server 指定path下的文件列表
        /// </summary>
        /// <param name="path">server path</param>
        /// <returns></returns>
        public SocketFileInfo[] RequestDirectory(string path)
        {
            SendBytes(client, SocketPacketFlag.DirectoryRequest, path);
            ReceiveBytes(client, out HB32Header header, out byte[] bytes);
            if (header.Flag != SocketPacketFlag.DirectoryResponse)
            {
                throw new Exception(Encoding.UTF8.GetString(bytes));
            }
            SendHeader(client, SocketPacketFlag.DirectoryRequest);
            ReceiveBytes(client, out _, out bytes);
            return SocketFileInfo.BytesToList(bytes);
        }


        /// <summary>
        /// client 异步 connect, 连接成功后执行回调函数句柄
        /// </summary>
        /// <param name="asyncCallback"></param>
        public void AsyncConnect(SocketAsyncCallback asyncCallback)
        {
            IPEndPoint ipe = new IPEndPoint(Hostip, Port);
            client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            client.SendTimeout = SocketClient.SendTimeout;
            client.ReceiveTimeout = SocketClient.ReceiveTimeout;
            client.BeginConnect(ipe, asyncResult => {
                try
                {
                    client.EndConnect(asyncResult);
                    asyncCallback();
                }
                catch(Exception ex)
                {
                    asyncExceptionCallback(ex);
                }
            }, null);
        }

        public void Connect()
        {
            IPEndPoint ipe = new IPEndPoint(Hostip, Port);
            client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            client.SendTimeout = SocketClient.SendTimeout;
            client.ReceiveTimeout = SocketClient.ReceiveTimeout;
            client.Connect(ipe);
            //client.Blocking = true;
        }

        public void Close()
        {
            client.Close();
        }


    }

}
