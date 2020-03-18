using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;



namespace SocketFileManager.SocketLib
{
    public class SocketClient : SocketIO
    {
        public Socket client = null;

        protected IPAddress Hostip;
        protected int Port;

        private SocketAsyncExceptionCallback asyncExceptionCallback = null;

        public SocketClient(string ip, string port, SocketAsyncExceptionCallback c = null)
            : this(IPAddress.Parse(ip), int.Parse(port), c)
        {
            ;
        }

        public SocketClient(string ip, int port, SocketAsyncExceptionCallback c = null) : this(IPAddress.Parse(ip), port, c)
        {

        }

        public SocketClient(IPAddress ip, int port, SocketAsyncExceptionCallback c = null)
        {
            Hostip = ip;
            Port = port;
            asyncExceptionCallback = c;
        }
        
        /// <summary>
        /// 获取 server 指定path下的文件列表
        /// </summary>
        /// <param name="path">server path</param>
        /// <returns></returns>
        public SokcetFileClass[] RequestDirectory(string path)
        {
            return RequestDirectory(client, path);
        }


        /// <summary>
        /// client 异步 connect, 连接成功后执行回调函数句柄
        /// </summary>
        /// <param name="asyncCallback"></param>
        public void AsyncConnect(SocketAsyncCallback asyncCallback)
        {
            IPEndPoint ipe = new IPEndPoint(Hostip, Port);
            client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            client.SendTimeout = Config.SocketSendTimeOut;
            client.ReceiveTimeout = Config.SocketReceiveTimeOut;
            client.BeginConnect(ipe, asyncResult => {
                try
                {
                    client.EndConnect(asyncResult);
                    asyncCallback();
                }
                catch(Exception ex)
                {
                    asyncExceptionCallback(ex);
                }
            }, null);
        }




        public void Connect()
        {
            IPEndPoint ipe = new IPEndPoint(Hostip, Port);
            client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            client.SendTimeout = Config.SocketSendTimeOut;
            client.ReceiveTimeout = Config.SocketReceiveTimeOut;
            client.Connect(ipe);
            //client.Blocking = true;
        }

        public void Close()
        {
            try
            {
                //SendHeader(client, new HB32Header() { Flag = SocketDataFlag.DisconnectRequest });
                client.Close();
            }
            catch (Exception) {; }
        }

    }

}
