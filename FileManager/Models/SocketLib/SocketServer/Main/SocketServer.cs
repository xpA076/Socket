using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using FileManager.Events;
using FileManager.Exceptions;
using FileManager.Models;
using FileManager.Models.Serializable;
using FileManager.Models.SocketLib.Enums;
using FileManager.Models.SocketLib.Services;
using FileManager.Models.SocketLib.SocketIO;
using FileManager.Models.SocketLib.SocketServer.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FileManager.Models.SocketLib.SocketServer.Main
{

    public partial class SocketServer : SocketServerBase
    {
        public event SocketIdentityCheckEventHandler CheckIdentity;

        public readonly SocketServerConfig Config = new SocketServerConfig();

        private readonly FileResourceManager FileResourceManager = new FileResourceManager();

        private readonly PathTranslator PathTranslator = new PathTranslator();

        private readonly CertificateService CertificateService = Program.Provider.GetService<CertificateService>();

        //protected SocketServer() { }

        public SocketServer(IPAddress ip) : base(ip)
        {

        }

        //private readonly Dictionary<SocketResponder, SocketSessionInfo> ClientSessions = new Dictionary<SocketResponder, SocketSessionInfo>();



        private readonly Dictionary<int, SocketSession> Sessions = new Dictionary<int, SocketSession>();

        private readonly ReaderWriterLockSlim SessionsLock = new ReaderWriterLockSlim();

        /// <summary>
        /// 在 Socket.accept() 获取到的 client 在这里处理
        /// 这个函数为 client 的整个生存周期
        /// </summary>
        /// <param name="responderObject">client socket</param>
        /// 
        protected override void ReceiveData(object responderObject)
        {
            //this.ReceiveData_HB32(responderObject);
            this.ReceiveData_HB16(responderObject);
        }


        private void ResponeNothing(SocketResponder responder, byte[] bytes)
        {
            responder.SendBytes(PacketType.Null, new byte[1]);
        }


        private void DisposeClient(SocketResponder responder)
        {
            try
            {
                responder.Dispose();
            }
            catch (Exception) { }
            finally
            {
                //ClientSessions.Remove(responder);
            }
        }



    }
}
