using FileManager.Events;
using FileManager.SocketLib.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FileManager.SocketLib.SocketServer
{
    public class SocketReversedServer : SocketServer
    {
        public TCPAddress ProxyAddress { get; set; } = null;

        public Socket heartbeat_socket = null;

        private SocketLongConnectionMaintainer maintainer;

        public SocketReversedServer(TCPAddress proxy_address, string name)
        {
            ProxyAddress = proxy_address.Copy();
            maintainer = new SocketLongConnectionMaintainer(proxy_address, name);
        }



        public override void StartListening()
        {
            Thread th_listen = new Thread(ReversedServerListen);
            th_listen.IsBackground = true;
            th_listen.Start();
        }


        public void ReversedServerListen()
        {
            maintainer.StartLongConnection();
            try
            {
                while (flag_listen)
                {
                    Socket client = maintainer.Accept();
                    Thread th_receive = new Thread(ReceiveData);
                    th_receive.IsBackground = true;
                    th_receive.Start(client);
                    Thread.Sleep(20);
                }
            }
            catch (Exception ex)
            {
                Log("ReversedServerListen() exception: " + ex.Message, LogLevel.Error);
            }

        }
    }
}
