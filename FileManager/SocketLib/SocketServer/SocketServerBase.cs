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

namespace FileManager.SocketLib.SocketServer
{
    public class SocketServerBase : SocketIO
    {
        private Socket server = null;

        protected TCPAddress HostAddress { get; set; }


        protected bool flag_listen = true;

        protected bool flag_receive = true;

        protected SocketServerBase()
        {

        }



        public SocketServerBase(IPAddress ip)
        {
            HostAddress.IP = ip;
        }


        public void InitializeServer(int port)
        {
            HostAddress.Port = port;
            IPEndPoint ipe = new IPEndPoint(HostAddress.IP, port);
            //IPEndPoint ipe = new IPEndPoint(HostIP, 12139);
            server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            server.Bind(ipe);
            server.Listen(20);
            Log(string.Format("Server initiated - {0}:{1}", HostAddress.IP, port), LogLevel.Info);
        }


        public virtual void StartListening()
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


        protected virtual void ReceiveData(object acceptSocketObject)
        {

        }

        public void Close()
        {
            server.Close();
        }
    }
}
