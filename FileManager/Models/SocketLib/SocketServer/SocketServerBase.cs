using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FileManager.Events;
using FileManager.Models.SocketLib.Enums;
using FileManager.Models.SocketLib.Models;
using FileManager.Models.SocketLib.SocketIO;

namespace FileManager.Models.SocketLib.SocketServer
{
    public class SocketServerBase
    {
        private Socket server = null;

        protected TCPAddress HostAddress { get; set; } = null;


        protected bool flag_listen = true;

        protected bool flag_receive = true;

        protected SocketServerBase()
        {

        }



        public SocketServerBase(IPAddress ip)
        {
            HostAddress = new TCPAddress()
            {
                IP = ip
            };
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
                    Socket client = server.Accept();
                    SocketResponder responder = new SocketResponder(client);
                    Thread th_receive = new Thread(ReceiveData);
                    th_receive.IsBackground = true;
                    th_receive.Start(responder);
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
