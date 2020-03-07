using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;



namespace SocketLib
{
    public class SocketClient : SocketIO
    {
        public Socket client = null;
        public Socket server = null;

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
        /*
        #region SocketIO 中函数封装

        public void ReceivePackage(out HB32Header header, out byte[] bytes_data)
        {
            ReceivePackage(client, out header, out bytes_data);
        }

        public void SendBytes(HB32Header header, byte[] bytes)
        {
            SendBytes(client, header, bytes);
        }

        public void SendBytes(HB32Header header, string str)
        {
            SendBytes(client, header, str);
        }

        public void ReceiveBytes(out HB32Header header, out byte[] bytes)
        {
            ReceiveBytes(client, out header, out bytes);
        }

        public void SendJson(HB32Header header, object obj)
        {
            SendJson(client, header, obj);
        }

        public void ReceiveJson<T>(out HB32Header header, out T obj)
        {
            ReceiveJson(client, out header, out obj);
        }

        public void Disconnect()
        {
            Disconnect(client);
        }

        public FileClass[] RequestDirectory(string path, out string message)
        {
            return RequestDirectory(client, path, out message);
        }

        public void ResponseDirectory(byte[] bytes)
        {
            ResponseDirectory(client, bytes);
        }

        public void FileDownload(string remotePath, string localPath)
        {
            FileDownload(client, remotePath, localPath);
        }

        public void FileDownloadResponse(byte[] bytes)
        {
            FileDownloadResponse(client, bytes);
        }

        public void FileUpload(string remotePath, string localPath)
        {
            FileUpload(client, remotePath, localPath);
        }

        public void FileUploadResponse(byte[] bytes)
        {
            FileUploadResponse(client, bytes);
        }

        public void FileDelete(string remotePath)
        {
            FileDelete(client, remotePath);
        }

        public void FileDeleteResponse(byte[] bytes)
        {
            FileDeleteResponse(client, bytes);
        }

        public void RemoteRunRequest(string remotePath)
        {
            RemoteRunRequest(client, remotePath);
        }

        public void RemoteRunResponse(byte[] bytes)
        {
            RemoteRunResponse(client, bytes);
        }

        #endregion
        */
        /// <summary>
        /// 获取 server 指定path下的文件列表
        /// </summary>
        /// <param name="path">server path</param>
        /// <returns></returns>
        public SokcetFileClass[] RequestDirectory(string path)
        {
            SokcetFileClass[] files = RequestDirectory(client, path, out string message);
            if (!string.IsNullOrEmpty(message)) { throw new Exception(message); }
            return files;
        }


        /// <summary>
        /// client 异步 connect, 连接成功后执行回调函数句柄
        /// </summary>
        /// <param name="asyncCallback"></param>
        public void AsyncConnect(SocketAsyncCallback asyncCallback)
        {
            IPEndPoint ipe = new IPEndPoint(Hostip, Port);
            client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            client.SendTimeout = 3000;
            client.ReceiveTimeout = 3000;
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
            client.SendTimeout = 3000;
            client.ReceiveTimeout = 3000;
            client.Connect(ipe);
            //client.Blocking = true;
        }

        public void Close()
        {
            client.Close();
        }

    }

}
