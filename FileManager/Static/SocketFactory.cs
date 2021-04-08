using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

using SocketLib;
using FileManager.Models;
using System.Net;

namespace FileManager.Static
{
    public static class SocketFactory
    {
        public static TCPAddress TcpAddress { get; set; }


        /// <summary>
        /// 生成已连接成功的 SocketClient 对象
        /// 若连接失败则在 3s后 重启新连接直至连接成功
        /// </summary>
        /// <param name="maxTry"></param>
        /// <param name="retryInterval"></param>
        /// <returns></returns>
        public static SocketClient GenerateConnectedSocketClient(int maxTry = 1, int retryInterval = 3000)
        {
            return GenerateConnectedSocketClient(TcpAddress, maxTry, retryInterval);
        }

        public static SocketClient GenerateConnectedSocketClient(FileTask task, int maxTry = 1, int retryInterval = 3000)
        {
            return GenerateConnectedSocketClient(task.TcpAddress, maxTry, retryInterval);
        }



        public static SocketClient GenerateConnectedSocketClient(TCPAddress tcpAddress, int maxTry = 1, int retryInterval = 3000)
        {
            int tryCount = 0;
            while (true)
            {
                if (maxTry > 0 && tryCount >= maxTry)
                {
                    throw new Exception("exceed max try times");
                }
                try
                {
                    SocketClient client = new SocketClient(tcpAddress);
                    client.Connect();
                    return client;
                }
                catch (Exception)
                {
                    tryCount++;
                    Thread.Sleep(retryInterval);
                }
            }
        }

    }
}
