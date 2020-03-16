using SocketServerConsole;
using System;
using System.Collections.Generic;
using System.IO;
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
        public Socket server = null;

        private IPAddress Hostip;
        private int Port = 12138;

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
            server.Listen(20);
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
                    Thread.Sleep(20);
                }
            }
            catch (Exception ex)
            {
                Display.WriteLine("ServerListen() exception: " + ex.Message);
            }
        }

        private Dictionary<int, PointerRecord> pointers = new Dictionary<int, PointerRecord>();
        private int lastId = 0;

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
                        case SocketDataFlag.DownloadRequest:
                            //FileDownloadResponse(client, bytes);
                            // FileDownloadResponse
                            string path = Encoding.UTF8.GetString(bytes);
                            if (header.I3 == 1)
                            {
                                byte[] smallFileBytes = File.ReadAllBytes(path);
                                SendBytes(client, new HB32Header
                                {
                                    Flag = SocketDataFlag.DownloadAllowed
                                }, 
                                smallFileBytes);
                            }
                            lock (pointers)
                            {
                                List<int> ids = new List<int>(pointers.Keys);
                                for(int i = 0; i < ids.Count; ++i)
                                {
                                    PointerRecord p = pointers[ids[i]];
                                    if (p.ServerPath == path)
                                    {
                                        // 若该 FileStream 不在使用中 (10s空闲) 则释放
                                        if ((DateTime.Now - p.LastTime).Seconds > 10)
                                        {
                                            p.Pointer.Close();
                                            pointers.Remove(ids[i]);
                                            break;
                                        }
                                        else
                                        {
                                            SendBytes(client, new HB32Header { Flag = SocketDataFlag.DownloadDenied }, "file occupied");
                                        }
                                    }
                                }
                            }
                            try
                            {
                                FileInfo fif = new FileInfo(path);
                                FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read);
                                PointerRecord record = new PointerRecord
                                {
                                    Pointer = fs,
                                    ServerPath = path,
                                    Length = fif.Length,
                                };
                                int id;
                                lock (this)
                                {
                                    this.lastId++;
                                    id = this.lastId;
                                }
                                pointers.Add(id, record);
                                SendBytes(client, new HB32Header { Flag = SocketDataFlag.DownloadAllowed }, id.ToString());
                            }
                            catch (Exception ex)
                            {
                                SendBytes(client, new HB32Header { Flag = SocketDataFlag.DownloadDenied }, ex.Message);
                            }
                            break;
                        case SocketDataFlag.DownloadPackageRequest:
                            PointerRecord prc = pointers[header.I1];
                            FileStream serverStream = prc.Pointer;
                            if (header.I2 == -1)
                            {
                                serverStream.Close();
                                pointers.Remove(header.I1);
                                break;
                            }
                            long begin = (long)header.I2 * HB32Encoding.DataSize;
                            int length = HB32Encoding.DataSize; // 有效byte长度
                            if (begin + HB32Encoding.DataSize > prc.Length)
                            {
                                length = (int)(prc.Length - begin);
                            }
                            byte[] readBytes = new byte[length];
                            lock (serverStream)
                            {
                                serverStream.Seek(begin, SeekOrigin.Begin);
                                serverStream.Read(readBytes, 0, length);
                            }
                            prc.LastTime = DateTime.Now;
                            SendBytes(client, new HB32Header { Flag = SocketDataFlag.DownloadPackageResponse }, readBytes);
                            break;
                            /*
                        case SocketDataFlag.UploadBiasRequest:meiy
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
                            */
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
