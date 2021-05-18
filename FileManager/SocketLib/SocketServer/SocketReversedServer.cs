using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.SocketLib.SocketServer
{
    public class SocketReversedServer : SocketServer
    {
        public TCPAddress ProxyAddress { get; set; } = null;

        public Socket heartbeat_socket = null;


        public SocketReversedServer(IPAddress ip) : base(ip)
        {

        }


        public void StartListening()
        {

        }


        public void ReversedServerListen()
        {

        }
    }
}
