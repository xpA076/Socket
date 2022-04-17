﻿using FileManager.SocketLib.Enums;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.SocketLib
{
    /// <summary>
    /// 代理通信主动端, 与上级通信
    /// </summary>
    public class SocketSender : SocketEndPoint
    {
        public SocketSender(bool isWithProxy)
        {
            this.IsRequireProxyHeader = isWithProxy;
        }

        public SocketSender(Socket socket, bool isWithProxy)
        {
            this.client = socket;
            this.IsRequireProxyHeader = isWithProxy;
        }


        public SocketResponder ConvertToResponder()
        {
            SocketResponder responder = new SocketResponder(this.client);
            return responder;
        }



        public override void Close()
        {
            try
            {
                SendHeader(HB32Packet.DisconnectRequest);
                client.Close();
            }
            catch { }
        }

    }
}
