using SocketServerConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace SocketLib
{
    public class SocketServer : SocketIO
    {
        public Socket client = null;
        public Socket server = null;

        private IPAddress Hostip;
        private int Port = 12345;

        public SocketServer(IPAddress ip)
        {
            Hostip = ip;
        }

        public SocketServer(IPAddress ip, int port)
        {
            Hostip = ip;
            Port = port;
        }



        public void InitializeServer()
        {
            IPEndPoint ipe = new IPEndPoint(Hostip, Port);
            server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            server.Bind(ipe);
            server.Listen(10);
        }


        private bool flag_listen = true;
        private bool flag_receive = true;

        public void ServerListen()
        {
            try
            {
                while (flag_listen)
                {
                    // 等待client连接时, 代码阻塞在此
                    Socket client = server.Accept();
                    // 可以在这里通过字典记录所有已连接socket
                    // 参考 https://www.cnblogs.com/kellen451/p/7127670.html
                    Display.TimeWriteLine("client connected");
                    Thread th_receive = new Thread(ReceiveData);
                    th_receive.IsBackground = true;
                    th_receive.Start(client);
                    Thread.Sleep(50);
                }
            }

            catch (Exception ex)
            {
                Display.WriteLine("ServerListen() exception: " + ex.Message);
            }
        }

        public void ReceiveData(object acceptSocketObject)
        {
            Socket client = (Socket)acceptSocketObject;
            client.SendTimeout = 3000;
            client.ReceiveTimeout = 3000;
            int error_count = 0;
            while (flag_receive & error_count < 5)
            {
                try
                {
                    ReceiveBytes(client, out HB32Header header, out byte[] bytes);
                    switch (header.Flag)
                    {
                        case SocketDataFlag.DirectoryRequest:
                            ResponseDirectory(client, bytes);
                            Display.TimeWriteLine("Directory request");
                            break;
                        case SocketDataFlag.DownloadBiasRequest:
                            FileDownloadResponse(client, header, bytes);
                            break;
                        case SocketDataFlag.DownloadStreamRequest:
                            // 应该不会收到此包头
                            // 若收到则丢出exception
                            throw new Exception("Invalid socket header in receiving: " + header.Flag.ToString());
                        case SocketDataFlag.UploadBiasRequest:
                            FileUploadResponse(client, header, bytes);
                            break;
                        case SocketDataFlag.UploadStreamRequest:
                            // 应该不会收到此包头
                            throw new Exception("Invalid socket header in receiving: " + header.Flag.ToString());
                        case SocketDataFlag.DeleteRequest:
                            FileDeleteResponse(client, bytes);
                            break;
                        case SocketDataFlag.RemoteRunRequest:
                            //RemoteRunResponse(client, bytes);
                            break;
                        case SocketDataFlag.DisconnectRequest:
                            client.Close();
                            return;
                        default:
                            throw new Exception("Invalid socket header in receiving: " + header.Flag.ToString());
                    }
                    error_count = 0;
                }
                catch (SocketException ex)
                {
                    error_count++;
                    switch (ex.ErrorCode)
                    {
                        // 远程 client 主机关闭连接
                        case 10054:
                            client.Close();
                            Display.WriteLine("connection closed (remote closed)");
                            return;
                        // Socket 超时
                        case 10060:
                            Thread.Sleep(200);
                            continue;
                        default:
                            //System.Windows.Forms.MessageBox.Show("Server receive data :" + ex.Message);
                            Display.WriteLine("Server receive data :" + ex.Message);
                            continue;
                    }
                }
                catch (Exception ex)
                {
                    error_count++;
                    if(ex.Message.Contains("Buffer receive error: cannot receive package"))
                    {
                        client.Close();
                        Display.TimeWriteLine("connection closed (buffer received none)");
                        return;
                    }
                    if (ex.Message.Contains("Invalid socket header"))
                    {
                        client.Close();
                        Display.TimeWriteLine("connection closed : " + ex.Message);
                        return;
                    }
                    Display.WriteLine("Server exception :" + ex.Message);
                    Thread.Sleep(200);
                    continue;
                }
            }
            Display.WriteLine("Connection closed: error count 5");
        }



        public void Close()
        {
            server.Close();
        }
    }
}
