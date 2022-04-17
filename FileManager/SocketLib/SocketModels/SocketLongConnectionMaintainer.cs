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
    public class SocketLongConnectionMaintainer : SocketEndPoint
    {
        public string Name { get; set; }

        public bool IsKeepLongConnection { get; set; } = true;

        public bool IsAccepting { get; set; } = true;

        public int DefaultSendTimeout { get; set; } = 3000;

        public int DefaultReceiveTimeout { get; set; } = 3000;

        public int LongConnectionTimeout { get; set; } = 20 * 1000;

        public int BuildConnectionTimeout { get; set; } = 2000;

        public int ReconnectInterval { get; set; } = 3000;


        private SocketSender LongConnectSender;


        private ConnectionRoute CurrentRoute { get; set; }



        public SocketLongConnectionMaintainer(ConnectionRoute route)
        {
            CurrentRoute = route.Copy();
        }
        public void StartLongConnection()
        {
            while (IsKeepLongConnection)
            {
                try
                {
                    LongConnectSender = new SocketSender(CurrentRoute.IsNextNodeProxy);
                    LongConnectSender.ConnectWithTimeout(CurrentRoute.NextNode.Address, BuildConnectionTimeout);
                    LongConnectSender.SetTimeout(LongConnectionTimeout, LongConnectionTimeout);
                    if (CurrentRoute.IsNextNodeProxy)
                    {
                        LongConnectSender.SendBytes(HB32Packet.ReversedProxyLongConnectionRequest, CurrentRoute.GetBytes(node_start_index: 1));
                        LongConnectSender.ReceiveBytes(out HB32Header header, out byte[] bytes);
                        if (header.Flag != HB32Packet.ProxyResponse)
                        {
                            throw new Exception(string.Format("Proxy exception at depth {0} : {1}. {2}",
                                header.I1, CurrentRoute.ProxyRoute[header.I1], Encoding.UTF8.GetString(bytes)));
                        }

                    }
                    return;
                }
                catch (Exception ex)
                {
                    Log("Start long connection exception : " + ex.Message, LogLevel.Error);
                }
                Thread.Sleep(ReconnectInterval);
            }
        }


        public SocketResponder Accept()
        {
            while (IsAccepting)
            {
                HB32Header query_header;
                try
                {
                    LongConnectSender.SendHeader(HB32Packet.ReverserProxyLongConnectionQuery);
                    LongConnectSender.ReceiveBytes(out query_header, out _);
                }
                catch(Exception ex)
                {
                    Log("Long connection exception : " + ex.Message, LogLevel.Warn);
                    StartLongConnection();
                    continue;
                }
                if (query_header.I1 == 1)
                {
                    try
                    {
                        SocketResponder responder = new SocketResponder();
                        responder.ConnectWithTimeout(CurrentRoute.NextNode.Address, BuildConnectionTimeout);
                        responder.SetTimeout(DefaultSendTimeout, DefaultReceiveTimeout);
                        if (CurrentRoute.IsNextNodeProxy)
                        {
                            responder.SendBytes(HB32Packet.ReversedProxyConnectionRequest, CurrentRoute.GetBytes(node_start_index: 1), i1: 0);
                            LongConnectSender.ReceiveBytes(out HB32Header header, out byte[] bytes);
                            if (header.Flag != HB32Packet.ProxyResponse)
                            {
                                throw new Exception(string.Format("Proxy exception at depth {0} : {1}. {2}",
                                    header.I1, CurrentRoute.ProxyRoute[header.I1], Encoding.UTF8.GetString(bytes)));
                            }
                        }
                        return responder;
                    }
                    catch (Exception ex)
                    {
                        Log("Reversed server Accept() exception : " + ex.Message, LogLevel.Error);
                    }
                }
            }
            return null;
        }





        public event SocketLogEventHandler SocketLog;

        private readonly object LoggerLock = new object();

        protected void Log(string info, LogLevel logLevel)
        {
            // 必须要加锁保证Log文件写时不被占用
            lock (LoggerLock)
            {
                SocketLog?.Invoke(this, new SocketLogEventArgs(info, logLevel));
            }
        }

    }
}
