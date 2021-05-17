using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

using FileManager.SocketLib.SocketServer;
using FileManager.SocketLib.Enums;


namespace FileManager.SocketLib.SocketProxy
{
    public class SocketProxy : SocketServerBase
    {

        public SocketProxyConfig Config { get; set; } = new SocketProxyConfig();

        public SocketProxy(IPAddress ip) : base(ip)
        {

        }

        public override void ReceiveData(object acceptSocketObject)
        {
            Socket client = (Socket)acceptSocketObject;
            try
            {
                client.SendTimeout = Config.SocketSendTimeOut;
                client.ReceiveTimeout = Config.SocketReceiveTimeOut;
                byte[] proxy_header = ReceiveProxyHeader(client);
                this.ReceiveBytes(client, out HB32Header route_header, out byte[] route_bytes);
                // assert: route_bytes[0] == 1
                TCPAddress aim;
                bool IsAimProxy;
                byte[] toSend;
                if (route_bytes[1] == 0)
                {
                    IsAimProxy = false;
                    aim = TCPAddress.FromBytes(route_bytes, 2);
                }




                
            }
            catch (Exception ex)
            {
                Log("Socket initiate exception :" + ex.Message, LogLevel.Error);
            }
        }


        /// <summary>
        /// Receive proxy 包头标志
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        private byte[] ReceiveProxyHeader(Socket client)
        {
            byte[] bytes = new byte[2];
            this.ReceiveBuffer(client, bytes, 2);
            return bytes;
        }


        private void GetAimInfo(byte[] recv_bytes, out TCPAddress address, out bool is_aim_proxy, out byte[] bytes_to_send)
        {
            
        }

    }
}
