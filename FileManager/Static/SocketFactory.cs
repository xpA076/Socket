using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

using SocketLib;
using SocketLib.Enums;
using FileManager.Models;
using System.Net;

namespace FileManager.Static
{
    public static class SocketFactory
    {
        public static TCPAddress ServerAddress { get; set; }

        public static TCPAddress ProxyAddress { get; set; } = null;

        public static ConnectionRoute CurrentRoute { get; set; } = null;





        // ******* todo *********** 21.05.11 rebuild with proxy





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


        private static SocketClient GenerateSocketClient(ConnectionRoute route, out byte[] bytes_to_send)
        {
            byte[] key_bytes = Config.KeyBytes;
            if (route.ProxyRoute.Count == 0)
            {
                SocketClient client = new SocketClient(route.ServerAddress);
                client.IsWithProxy = false;
                bytes_to_send = key_bytes;
                return client;
            }
            else
            {
                SocketClient client = new SocketClient(route.ProxyRoute[0]);
                client.IsWithProxy = true;
                byte[] proxy_bytes = route.GetBytesExceptFirstProxy();
                bytes_to_send = new byte[proxy_bytes.Length + key_bytes.Length];
                Array.Copy(proxy_bytes, bytes_to_send, proxy_bytes.Length);
                Array.Copy(key_bytes, 0, bytes_to_send, proxy_bytes.Length, key_bytes.Length);
                return client;
            }
        }


        public static SocketClient GenerateConnectedSocketClient(ConnectionRoute route, int maxTry = 1, int retryInterval = 3000)
        {
            int tryCount = 0;
            while (true)
            {
                if (maxTry > 0 && tryCount >= maxTry)
                {
                    throw new ArgumentException("exceed max try times");
                }
                try
                {
                    SocketClient client = GenerateSocketClient(route, out byte[] bytes_to_send);
                    client.Connect(Config.SocketSendTimeout, Config.SocketReceiveTimeout);
                    client.SendBytes(SocketPacketFlag.AuthenticationPacket, bytes_to_send, 0, 0, 1);
                    client.ReceiveBytesWithHeaderFlag(SocketPacketFlag.AuthenticationResponse, out HB32Header header);
                    return client;
                }
                catch (Exception)
                {
                    tryCount++;
                    Thread.Sleep(retryInterval);
                }
            }
        }


        public static SocketIdentity AsyncConnectForIndetity(ConnectionRoute route, SocketAsyncCallback asyncCallback, SocketAsyncExceptionCallback exceptionCallback)
        {
            SocketClient client = GenerateSocketClient(route, out byte[] bytes_to_send);
            SocketIdentity identity = SocketIdentity.None;
            client.AsyncConnect(()=> {
                client.SendBytes(SocketPacketFlag.AuthenticationPacket, bytes_to_send, 0, 0, 1);
                client.ReceiveBytesWithHeaderFlag(SocketPacketFlag.AuthenticationResponse, out HB32Header header);
                identity = (SocketIdentity)header.I1;
                client.Close();
                asyncCallback();
            }, exceptionCallback, Config.SocketSendTimeout, Config.SocketReceiveTimeout);
            return identity;
        }




        /*
        public static Communicator GetConnectedCommunicator(TCPAddress server_address, int maxTry = -1, int retryInterval = 3000)
        {

        }


        public static Communicator GetConnectedCommunicator(TCPAddress server_address, TCPAddress proxy_address, int maxTry = -1, int retryInterval = 3000)
        {

        }

        */
    }
}
