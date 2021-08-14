using FileManager.Exceptions;
using FileManager.SocketLib.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FileManager.SocketLib
{
    /// <summary>
    /// Socket 连接中的 client / server 端, 有简单封装的 Connect / SendBytes / ReceiveBytes 方法
    /// </summary>

    public class SocketEndPoint
    {
        protected Socket client = null;

        /// <summary>
        /// 当前 SocketEndPoint 是否为向代理端主动通信的对象
        /// 这种情况下数据通信需要额外添加代理包头
        /// </summary>
        public bool IsRequireProxyHeader { get; protected set; } = false;


        public void SetTimeout(int send_timeout, int receive_timeout)
        {
            client.SendTimeout = send_timeout;
            client.ReceiveTimeout = receive_timeout;
        }




        #region Send / Receive


        public void SendHeader(HB32Header header)
        {
            if (IsRequireProxyHeader)
            {
                SocketIO.SendHeader(client, header, new byte[2] { SocketProxy.ProxyHeaderByte, (byte)ProxyHeader.SendHeader });
            }
            else
            {
                SocketIO.SendHeader(client, header, new byte[2] { 0, 0 });
            }
        }


        public void SendHeader(SocketPacketFlag flag, int i1 = 0, int i2 = 0, int i3 = 0)
        {
            SendHeader(new HB32Header
            {
                Flag = flag,
                I1 = i1,
                I2 = i2,
                I3 = i3
            });
        }


        public void SendBytes(HB32Header header, byte[] bytes)
        {
            if (IsRequireProxyHeader)
            {
                SocketIO.SendBytes(client, header, bytes, new byte[2] { SocketProxy.ProxyHeaderByte, (byte)ProxyHeader.SendBytes });
            }
            else
            {
                SocketIO.SendBytes(client, header, bytes, new byte[2] { 0, 0 });
            }
        }

        public void SendBytes(HB32Header header, string str)
        {
            SendBytes(header, Encoding.UTF8.GetBytes(str));
        }




        public void SendBytes(SocketPacketFlag flag, byte[] bytes, int i1 = 0, int i2 = 0, int i3 = 0)
        {
            SendBytes(new HB32Header
            {
                Flag = flag,
                I1 = i1,
                I2 = i2,
                I3 = i3
            }, bytes);
        }

        public void SendBytes(SocketPacketFlag flag, string str, int i1 = 0, int i2 = 0, int i3 = 0)
        {
            SendBytes(flag, Encoding.UTF8.GetBytes(str), i1, i2, i3);
        }


        public ProxyHeader ReceiveProxyHeader()
        {
            byte[] proxy_bytes = new byte[2];
            SocketIO.ReceiveBuffer(client, proxy_bytes);
            if (proxy_bytes[0] == 0x01)
            {
                return (ProxyHeader)proxy_bytes[1];
            }
            return ProxyHeader.None;
        }


        public void ReceiveBytes(out HB32Header header, out byte[] bytes)
        {
            if (IsRequireProxyHeader)
            {
                client.Send(new byte[2] { 0xA3, (byte)ProxyHeader.ReceiveBytes });
            }
            /// Receive 的数据仍有一个空的ProxyHeader, 应处理后再接收数据
            ReceiveProxyHeader();
            SocketIO.ReceiveBytes(client, out header, out bytes);
        }

        public byte[] ReceiveBuffer(int length)
        {
            byte[] bs = new byte[length];
            SocketIO.ReceiveBuffer(client, bs);
            //throw new Exception("cannot use this method");
            return bs;
        }


        public void ReceiveBytesWithHeaderFlag(SocketPacketFlag flag, out HB32Header header, out byte[] bytes)
        {
            ReceiveBytes(out header, out bytes);
            if (header.Flag != flag)
            {
                throw new SocketFlagException(flag, header, bytes);
            }
        }


        public void ReceiveBytesWithHeaderFlag(SocketPacketFlag flag)
        {
            ReceiveBytesWithHeaderFlag(flag, out _, out _);
        }

        public void ReceiveBytesWithHeaderFlag(SocketPacketFlag flag, out HB32Header header)
        {
            ReceiveBytesWithHeaderFlag(flag, out header, out _);
        }


        public void ReceiveBytesWithHeaderFlag(SocketPacketFlag flag, out byte[] bytes)
        {
            ReceiveBytesWithHeaderFlag(flag, out _, out bytes);
        }

        public static void CheckFlag(SocketPacketFlag required_flag, HB32Response response)
        {
            if (response.Header.Flag != required_flag)
            {
                throw new SocketFlagException(required_flag, response);
            }
        }

        #endregion


        public void Connect(TCPAddress address)
        {
            IPEndPoint ipe = new IPEndPoint(address.IP, address.Port);
            client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            client.Connect(ipe);
        }


        private class ConnectTimeoutHandler
        {
            private readonly ManualResetEvent ConnectTimeoutObject = new ManualResetEvent(false);

            public bool IsSuccess { get; set; } = false;

            public Exception ConnectException { get; set; } = new Exception("null connect exception");


            public void Set()
            {
                ConnectTimeoutObject.Set();
            }

            public void Reset()
            {
                ConnectTimeoutObject.Reset();
            }

            public bool WaitOne(int millisecondsTimeout, bool exitContext)
            {
                return ConnectTimeoutObject.WaitOne(millisecondsTimeout, exitContext);
            }


        }

        private readonly ConnectTimeoutHandler cth = new ConnectTimeoutHandler();



        public void ConnectWithTimeout(TCPAddress address, int timeout)
        {
            cth.Reset();
            IPEndPoint ipe = new IPEndPoint(address.IP, address.Port);
            client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            client.BeginConnect(ipe, asyncResult =>
            {
                try
                {
                    cth.IsSuccess = false;
                    if (asyncResult.AsyncState is Socket s)
                    {
                        s.EndConnect(asyncResult);
                        cth.IsSuccess = true;
                    }
                }
                catch(Exception ex)
                {
                    cth.IsSuccess = false;
                    cth.ConnectException = ex;
                }
                finally
                {
                    cth.Set();
                }
            }, client);
            if (cth.WaitOne(timeout, false))
            {
                if (cth.IsSuccess)
                {
                    return;
                }
                else
                {
                    throw cth.ConnectException;
                }

            }
            else
            {
                client.Close();
                throw new TimeoutException("Connection timeout");
            }

        }



        public void Connect(TCPAddress address, int send_timeout, int recv_timeout)
        {
            IPEndPoint ipe = new IPEndPoint(address.IP, address.Port);
            client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            client.SendTimeout = send_timeout;
            client.ReceiveTimeout = recv_timeout;
            client.Connect(ipe);
        }

        public virtual void Close()
        {
            CloseSocket();
        }


        public void CloseSocket()
        {
            try
            {
                client.Close();
            }
            catch { }
        }
    }
}