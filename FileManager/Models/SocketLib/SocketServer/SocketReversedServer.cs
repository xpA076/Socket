using FileManager.Events;
using FileManager.Models.SocketLib.Enums;
using FileManager.Models.SocketLib.Models;
using FileManager.Models.SocketLib.SocketIO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FileManager.Models.SocketLib.SocketServer
{
    public class SocketReversedServer : SocketServerBase
    {
        /// <summary>
        /// 若此反向代理服务器直接挂载在 IP 1.2.3.4:10000 上名为 abc
        /// RouteToProxy 应为 
        ///     ServerAddress    1.2.3.4:10000-abc
        ///     ProxyRoute       [1.2.3.4:10000]
        /// </summary>
        private ConnectionRoute RouteToProxy { get; set; }



        private SocketLongConnectionMaintainer maintainer = null;

        public SocketReversedServer(ConnectionRoute route)
        {
            RouteToProxy = route.Copy();
            maintainer = new SocketLongConnectionMaintainer(route);
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
                    SocketResponder responder = maintainer.Accept();
                    Thread th_receive = new Thread(ReceiveData);
                    th_receive.IsBackground = true;
                    th_receive.Start(responder);
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
