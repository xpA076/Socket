using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.SocketLib
{
    public class SocketLongConnectionMaintainer : SocketIO
    {
        

        public Socket long_connection_socket;

        public TCPAddress ServerAddres { get; set; }

        public void Connect()
        {
            this.long_connection_socket = GenerateConnectedSocket();
        }


        private Socket GenerateConnectedSocket()
        {
            IPEndPoint ipe = new IPEndPoint(ServerAddres.IP, ServerAddres.Port);
            Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            s.SendTimeout = 20 * 1000;
            s.ReceiveTimeout = 20 * 1000;
            s.Connect(ipe);
            return s;
        }


        public Socket Accept()
        {
            while (true)
            {
                this.long_connection_socket.Send(new byte[] { 0xA4, 0x00 });
                byte[] received_header = new byte[2];
                this.ReceiveBuffer(this.long_connection_socket, received_header);
                if (received_header[1] == 0x01)
                {
                    return GenerateConnectedSocket();
                }
            }
        }


    }
}
