using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


using FileManager.SocketLib.Enums;

namespace FileManager.SocketLib.SocketServer
{
    public class SocketServerBase : SocketIO
    {
        public Socket server = null;

        

        public IPAddress HostIP { get; set; }

        

        public SocketLogger Logger { get; set; } = null;

        private readonly object LoggerLock = new object();

        protected bool flag_listen = true;

        protected bool flag_receive = true;

        protected void Log(string info, LogLevel logLevel)
        {
            if (Logger != null)
            {
                // 必须要加锁保证Log文件写时不被占用
                lock (LoggerLock)
                {
                    Logger(info, logLevel);
                }
            }
        }



        public SocketServerBase(IPAddress ip)
        {
            HostIP = ip;
        }


        public void InitializeServer(int port)
        {
            IPEndPoint ipe = new IPEndPoint(HostIP, port);
            //IPEndPoint ipe = new IPEndPoint(HostIP, 12139);
            server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            server.Bind(ipe);
            server.Listen(20);
            Log(string.Format("Server initiated - {0}:{1}", HostIP, port), LogLevel.Info);
        }


        public void StartListening()
        {
            Thread th_listen = new Thread(ServerListen);
            th_listen.IsBackground = true;
            th_listen.Start();
        }


        public void ServerListen()
        {
            try
            {
                while (flag_listen)
                {
                    // 等待client连接时, 代码阻塞在此
                    Socket client = server.Accept();
                    // 可以在这里通过字典记录所有已连接socket
                    // 参考 https://www.cnblogs.com/kellen451/p/7127670.html
                    Thread th_receive = new Thread(ReceiveData);
                    th_receive.IsBackground = true;
                    th_receive.Start(client);
                    Thread.Sleep(20);
                }
            }
            catch (Exception ex)
            {
                Log("ServerListen() exception: " + ex.Message, LogLevel.Error);
            }
        }


        public virtual void ReceiveData(object acceptSocketObject)
        {

        }

        public void Close()
        {
            server.Close();
        }
    }
}
