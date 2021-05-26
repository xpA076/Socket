using FileManager.SocketLib.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.SocketLib
{
    /// <summary>
    /// 代理服务器用于与上级服务器通信的对象
    /// </summary>
    public class SocketProxyClient : SocketEndPoint
    {
        public SocketProxyClient(Socket socket)
        {
            this.client = socket;
        }



        public override void Close()
        {
            try
            {
                SendHeader(SocketPacketFlag.DisconnectRequest);
                client.Close();
            }
            catch { }
        }
    }
}
