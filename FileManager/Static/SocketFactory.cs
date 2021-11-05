using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net;

using FileManager.SocketLib;
using FileManager.SocketLib.Enums;
using FileManager.Models;
using System.Net.Sockets;
using FileManager.SocketLib.Models;

namespace FileManager.Static
{
    public static class SocketFactory
    {
        public static ConnectionRoute CurrentRoute { get; set; } = null;



        /// <summary>
        /// 生成已连接成功的 SocketClient 对象
        /// 若连接失败则在 3s后 重启新连接直至连接成功
        /// </summary>
        /// <param name="maxTry"></param>
        /// <param name="retryInterval"></param>
        /// <returns></returns>
        public static SocketClient GenerateConnectedSocketClient(int maxTry = 1, int retryInterval = 3000)
        {
            return GenerateConnectedSocketClient(CurrentRoute, maxTry, retryInterval);
        }

        public static SocketClient GenerateConnectedSocketClient(FileTask task, int maxTry = 1, int retryInterval = 3000)
        {
            return GenerateConnectedSocketClient(task.Route, maxTry, retryInterval);
        }





        public static SocketClient GenerateConnectedSocketClient(ConnectionRoute route)
        {
            SocketClient client = new SocketClient(route.NextNode, route.IsNextNodeProxy);
            //client.Connect(Config.SocketSendTimeout, Config.SocketReceiveTimeout);
            client.ConnectWithTimeout(Config.BuildConnectionTimeout);
            client.SetTimeout(Config.SocketSendTimeout, Config.SocketReceiveTimeout);
            if (route.IsNextNodeProxy)
            {
                /// 向代理服务器申请建立与服务端通信隧道, 并等待隧道建立完成
                client.SendBytes(SocketPacketFlag.ProxyRouteRequest, route.GetBytes(node_start_index: 1));
                client.ReceiveBytesWithHeaderFlag(SocketPacketFlag.ProxyResponse);
            }
            /// 获取 socket 权限
            client.SendBytes(SocketPacketFlag.AuthenticationRequest, Config.KeyBytes);
            client.ReceiveBytesWithHeaderFlag(SocketPacketFlag.AuthenticationResponse);
            return client;
        }






        /// <summary>
        /// 建立连接, (如通信需要经过代理, 会向代理发送后续路由信息并等待代理隧道建立完成)
        /// 而后发送 KeyBytes 获取 SocketIdentity
        /// 返回经过认证后的 SocketClient
        /// </summary>
        /// <param name="route"></param>
        /// <param name="maxTry">maxTry < 0 时表示无限次重复直到成功</param>
        /// <param name="retryInterval"></param>
        /// <returns></returns>
        public static SocketClient GenerateConnectedSocketClient(ConnectionRoute route, int maxTry, int retryInterval = 3000)
        {
            int tryCount = 0;
            string err_msg = "";
            while (true)
            {
                if (maxTry > 0 && tryCount >= maxTry)
                {
                    throw new ArgumentException("Generating valid socket failed : exceed max try times.\n" + err_msg);
                }
                try
                {
                    return GenerateConnectedSocketClient(route);
                }
                catch (Exception ex)
                {
                    err_msg += ex.Message + "\n";
                    tryCount++;
                    Thread.Sleep(retryInterval);
                }
            }
        }

        public static void AsyncConnectForIdentity(SocketAsyncCallbackEventHandler asyncCallback, SocketAsyncExceptionEventHandler exceptionCallback)
        {
            AsyncConnectForIdentity(CurrentRoute, asyncCallback, exceptionCallback);
        }

        public static void AsyncConnectForIdentity(ConnectionRoute route, SocketAsyncCallbackEventHandler asyncCallback, SocketAsyncExceptionEventHandler exceptionCallback)
        {
            Task.Run(() =>
            {
                try
                {
                    SocketIdentity identity = SocketIdentity.None;
                    SocketClient client = new SocketClient(route.NextNode, route.IsNextNodeProxy);
                    //client.Connect(Config.SocketSendTimeout, Config.SocketReceiveTimeout);
                    client.ConnectWithTimeout(Config.BuildConnectionTimeout);
                    client.SetTimeout(Config.SocketSendTimeout, Config.SocketReceiveTimeout);
                    if (route.IsNextNodeProxy)
                    {
                        /// 向代理服务器申请建立与服务端通信隧道, 并等待隧道建立完成
                        client.SendBytes(SocketPacketFlag.ProxyRouteRequest, route.GetBytes(node_start_index: 1), i1: 0);
                        client.ReceiveBytes(out HB32Header header, out byte[] bytes);
                        if (header.Flag != SocketPacketFlag.ProxyResponse)
                        {
                            // todo 捕获异常方式有问题 : header.I1 为 SeverAddress 时会越界, 应该要改CurrentRoute.ProxyRoute 21.06.04
                            throw new Exception(string.Format("Proxy exception at depth {0} : {1}. {2}", 
                                header.I1, route.ProxyRoute[header.I1], Encoding.UTF8.GetString(bytes)));
                        }
                    }
                    /// 获取 socket 权限
                    client.SendBytes(SocketPacketFlag.AuthenticationRequest, Config.KeyBytes);
                    client.ReceiveBytesWithHeaderFlag(SocketPacketFlag.AuthenticationResponse, out HB32Header auth_header);
                    identity = (SocketIdentity)auth_header.I1;
                    client.Close();
                    asyncCallback.Invoke(null, EventArgs.Empty);
                }
                catch (Exception ex)
                {
                    exceptionCallback.Invoke(null, new SocketAsyncExceptionEventArgs(ex));
                }
            });



            /*
             * 
             * 原来这种方案同样是异步执行, 总会提前返回 SocketIdentity.None
            SocketIdentity identity = SocketIdentity.None;
            SocketClient client = new SocketClient(route.NextNode, route.IsNextNodeProxy);
            client.SocketAsyncCallback += (object sender, EventArgs e) =>
            {
                if (route.IsNextNodeProxy)
                {
                    /// 向代理服务器申请建立与服务端通信隧道, 并等待隧道建立完成
                    client.SendBytes(SocketPacketFlag.ProxyRouteRequest, route.GetBytes(node_start_index: 1));
                    client.ReceiveBytesWithHeaderFlag(SocketPacketFlag.ProxyResponse);
                }
                /// 获取 socket 权限
                client.SendBytes(SocketPacketFlag.AuthenticationRequest, Config.KeyBytes);
                client.ReceiveBytesWithHeaderFlag(SocketPacketFlag.AuthenticationResponse, out HB32Header header);
                identity = (SocketIdentity)header.I1;
                client.Close();
            };
            client.SocketAsyncCallback += asyncCallback;
            client.SocketAsyncException += exceptionCallback;
            client.AsyncConnect(Config.SocketSendTimeout, Config.SocketReceiveTimeout);
            */
        }


        public static HB32Response Request(SocketPacketFlag flag, string str, int i1 = 0, int i2 = 0, int i3 = 0)
        {
            return Request(new HB32Header { Flag = flag, I1 = i1, I2 = i2, I3 = i3 }, Encoding.UTF8.GetBytes(str));
        }


        public static HB32Response Request(SocketPacketFlag flag, byte[] bytes, int i1 = 0, int i2 = 0, int i3 = 0)
        {
            return Request(new HB32Header { Flag = flag, I1 = i1, I2 = i2, I3 = i3 }, bytes);
        }


        public static HB32Response Request(HB32Header header, byte[] bytes)
        {
            SocketClient client = GenerateConnectedSocketClient();
            client.SendBytes(header, bytes);
            client.ReceiveBytes(out HB32Header h, out byte[] bs);
            client.Close();
            return new HB32Response(h, bs);
        }

        public static HB32Response RequestWithHeaderFlag(SocketPacketFlag requiredFlag, HB32Header header, byte[] bytes)
        {
            SocketClient client = GenerateConnectedSocketClient();
            client.SendBytes(header, bytes);
            client.ReceiveBytesWithHeaderFlag(requiredFlag, out HB32Header h, out byte[] bs);
            client.Close();
            return new HB32Response(h, bs);
        }


    }
}
