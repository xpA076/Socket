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

using SocketLib;

namespace ServerConsoleApp
{
    public class SocketServer : SocketIO
    {
        public Socket server = null;

        private IPAddress Hostip;

        private Dictionary<int, ServerFileInfoClass> ServerFileSet = new Dictionary<int, ServerFileInfoClass>();

        public SocketServer(IPAddress ip)
        {
            Hostip = ip;
        }

        public SocketServer(IPAddress ip, int port)
        {
            Hostip = ip;
            Config.ServerPort = port;
        }

        public void InitializeServer()
        {
            IPEndPoint ipe = new IPEndPoint(Hostip, Config.ServerPort);
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
                    //Display.TimeWriteLine("client connected");
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

        
        public void ReceiveData(object acceptSocketObject)
        {
            Socket client = (Socket)acceptSocketObject;
            client.SendTimeout = Config.SocketSendTimeOut;
            client.ReceiveTimeout = Config.SocketReceiveTimeOut;
            int error_count = 0;
            while (flag_receive & error_count < 5)
            {
                try
                {
                    ReceiveBytes(client, out HB32Header header, out byte[] bytes);
                    //Display.TimeWriteLine(header.Flag.ToString());
                    switch (header.Flag)
                    {
                        case SocketDataFlag.DirectoryRequest:
                            ResponseDirectory(client, bytes);
                            break;
                        case SocketDataFlag.DirectorySizeRequest:
                            ResponseDirectorySize(client, bytes);
                            break;

                        case SocketDataFlag.CreateDirectoryRequest:
                            ResponseCreateDirectory(client, bytes);
                            break;

                        #region download
                        case SocketDataFlag.DownloadRequest:
                            ResponseDownloadSmallFile(client, bytes);
                            break;
                        case SocketDataFlag.DownloadFileStreamIdRequest:
                            ResponseFileStreamId(client, header, bytes, false);
                            break;
                        case SocketDataFlag.DownloadPacketRequest:
                            ResponseTransferPacket(client, header, bytes, false);
                            break;
                        #endregion

                        #region upload
                        case SocketDataFlag.UploadRequest:
                            ResponseUploadSmallFile(client, bytes);
                            break;
                        case SocketDataFlag.UploadFileStreamIdRequest:
                            ResponseFileStreamId(client, header, bytes, true);
                            break;
                        case SocketDataFlag.UploadPacketRequest:
                            ResponseTransferPacket(client, header, bytes, true);
                            break;
                        #endregion

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
                        //Display.TimeWriteLine("connection closed (buffer received none)");
                        return;
                    }
                    if (ex.Message.Contains("Invalid socket header"))
                    {
                        client.Close();
                        Display.TimeWriteLine("connection closed : " + ex.Message);
                        return;
                    }
                    Display.TimeWriteLine("Server exception :" + ex.Message);
                    Thread.Sleep(200);
                    continue;
                }
            }
            Display.TimeWriteLine("Connection closed: error count 5");
        }

        protected override SocketFileInfo[] GetDirectoryAndFiles(string path)
        {
            List<SocketFileInfo> list = new List<SocketFileInfo>();
            if (string.IsNullOrEmpty(path))
            {
                foreach (string _path in Config.AllowDirectoryList)
                {
                    if (Directory.Exists(_path))
                    {
                        list.Add(new SocketFileInfo { Name = _path, IsDirectory = true });
                    }
                }
                return list.ToArray();
            }
            else
            {
                DirectoryInfo directory = new DirectoryInfo(path);
                FileInfo[] fileInfos = directory.GetFiles();
                DirectoryInfo[] directoryInfos = directory.GetDirectories();
                foreach (DirectoryInfo directoryInfo in directoryInfos)
                {
                    try
                    {
                        list.Add(new SocketFileInfo
                        {
                            Name = directoryInfo.Name,
                            Length = 0,
                            IsDirectory = true,
                        });
                    }
                    catch (Exception) {; }
                }
                foreach (FileInfo fileInfo in fileInfos)
                {
                    list.Add(new SocketFileInfo
                    {
                        Name = fileInfo.Name,
                        Length = fileInfo.Length,
                        IsDirectory = false,
                    });
                }
                list.Sort(SocketFileInfo.Compare);
                return list.ToArray();
            }
        }


        private void ResponseDirectorySize(Socket client, byte[] bytes)
        {
            string path = Encoding.UTF8.GetString(bytes);
            long size = GetDirectorySize(path);
            SendBytes(client, SocketDataFlag.DirectorySizeResponse, size.ToString());
        }


        private long GetDirectorySize(string path)
        {
            long size = 0;
            DirectoryInfo dir = new DirectoryInfo(path);
            FileInfo[] fileInfos = dir.GetFiles();
            foreach(FileInfo fileInfo in fileInfos)
            {
                if (Config.IsPathAllowed(fileInfo.FullName))
                {
                    size += fileInfo.Length;
                }
            }
            DirectoryInfo[] directoryInfos = dir.GetDirectories();
            foreach(DirectoryInfo directoryInfo in directoryInfos)
            {
                if (Config.IsPathAllowed(directoryInfo.FullName))
                {
                    size += GetDirectorySize(directoryInfo.FullName);
                }
            }
            return size;
        }



        private void ResponseCreateDirectory(Socket client, byte[] bytes)
        {
            /// 验证 key
            int keyLength = Config.KeyLength;
            byte[] key = new byte[keyLength];
            Array.Copy(bytes, 0, key, 0, keyLength);
            try
            {
                if (!CheckKey(key)) { throw new Exception("Key error"); }
                string path = BytesParser.ParseString(bytes, ref keyLength);
                if (!Directory.Exists(path))
                {
                    DirectoryInfo dirInfo = new DirectoryInfo(path);
                    dirInfo.Create();
                }
                SendBytes(client, new HB32Header { Flag = SocketDataFlag.CreateDirectoryAllowed }, new byte[1]);
            }
            catch (Exception ex)
            {
                SendBytes(client, new HB32Header { Flag = SocketDataFlag.CreateDirectoryDenied }, ex.Message);
            }
        }

        /// <summary>
        /// 小文件下载响应
        /// bytes内容仅为 server上文件路径
        /// 返回 SocketDataFlag.DownloadAllowed 和文件byte内容
        /// </summary>
        /// <param name="client"></param>
        /// <param name="bytes"></param>
        private void ResponseDownloadSmallFile(Socket client, byte[] bytes)
        {
            string path = Encoding.UTF8.GetString(bytes);
            byte[] _bytes;
            try
            {
                _bytes = File.ReadAllBytes(path);
            }
            catch (Exception ex)
            {
                SendBytes(client, new HB32Header { Flag = SocketDataFlag.DownloadDenied}, ex.Message);
                return;
            }
            SendBytes(client, new HB32Header { Flag = SocketDataFlag.DownloadAllowed}, _bytes);
        }

        /// <summary>
        /// 小文件上传响应
        /// byte 内容: 16-byte key, path string, content bytes
        /// 返回 SocketDataFlag.UploadAllowed 和空包(异常时返回异常信息)
        /// </summary>
        /// <param name="client"></param>
        /// <param name="bytes"></param>
        private void ResponseUploadSmallFile(Socket client, byte[] bytes)
        {
            int headerLength = Config.KeyLength;
            byte[] key = new byte[headerLength];
            Array.Copy(bytes, 0, key, 0, headerLength);
            string path = BytesParser.ParseString(bytes, ref headerLength);
            if (!CheckKey(key))
            {
                SendBytes(client, new HB32Header { Flag = SocketDataFlag.UploadDenied }, "key error");
                return;
            }
            byte[] contentBytes = new byte[bytes.Length - headerLength];
            Array.Copy(bytes, headerLength, contentBytes, 0, contentBytes.Length);
            try
            {
                File.WriteAllBytes(path, contentBytes);
            }
            catch (Exception ex)
            {
                SendBytes(client, new HB32Header { Flag = SocketDataFlag.UploadDenied}, ex.Message);
                return;
            }
            SendBytes(client, new HB32Header { Flag = SocketDataFlag.UploadAllowed}, new byte[1]);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="client"></param>
        /// <param name="header"></param>
        /// <param name="bytes"></param>
        /// <param name="isUpload"></param>
        private void ResponseTransferPacket(Socket client, HB32Header header, byte[] bytes, bool isUpload)
        {
            ServerFileInfoClass sfc = ServerFileSet[header.I1];
            FileStream fs = sfc.FStream;
            /// I2 == -1 则关闭 FileStream
            if (header.I2 == -1)
            {
                fs.Close();
                ServerFileSet.Remove(header.I1);
                Display.TimeWriteLine("Released file id : " + header.I1.ToString());
                return;
            }
            /// 确定有效 byte 长度
            long begin = (long)header.I2 * HB32Encoding.DataSize;
            int length = HB32Encoding.DataSize; // 有效byte长度
            if (begin + HB32Encoding.DataSize > sfc.Length)
            {
                length = (int)(sfc.Length - begin);
            }
            byte[] responseBytes = new byte[0];
            /// 定位 FileStream 读取/写入 bytes
            lock (fs)
            {
                fs.Seek(begin, SeekOrigin.Begin);
                if (isUpload)
                {
                    fs.Write(bytes, 0, header.ValidByteLength);
                    //Display.TimeWriteLine(header.I2.ToString());
                }
                else
                {
                    responseBytes = new byte[length];
                    fs.Read(responseBytes, 0, length);
                }
            }
            sfc.LastTime = DateTime.Now;
            /// response
            if (isUpload)
            {
                SendHeader(client, new HB32Header { Flag = SocketDataFlag.UploadPacketResponse });
            }
            else
            {
                SendBytes(client, new HB32Header { Flag = SocketDataFlag.DownloadPacketResponse }, responseBytes);
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="client"></param>
        /// <param name="header"></param>
        /// <param name="bytes"></param>
        /// <param name="isUpload"></param>
        private void ResponseFileStreamId(Socket client, HB32Header header, byte[] bytes, bool isUpload)
        {
            SocketDataFlag mask = (SocketDataFlag)((isUpload ? 1 : 0) << 8);
            string path;
            /// 验证 key
            if (isUpload)
            {
                int keyLength = Config.KeyLength;
                byte[] key = new byte[keyLength];
                Array.Copy(bytes, 0, key, 0, keyLength);
                path = BytesParser.ParseString(bytes, ref keyLength);
                if (!CheckKey(key)) 
                {
                    SendBytes(client, new HB32Header { Flag = SocketDataFlag.DownloadDenied ^ mask }, "key error");
                    return;
                }
            }
            else
            {
                path = Encoding.UTF8.GetString(bytes);
            }
            /// 验证文件是否被占用
            if (IsFileOccupying(path))
            {
                SendBytes(client, new HB32Header { Flag = SocketDataFlag.DownloadDenied ^ mask }, "file occupied");
                return;
            }
            /// 生成 FileStreamId 并记录
            try
            {
                FileInfo fif = new FileInfo(path);
                FileStream fs = new FileStream(path, FileMode.OpenOrCreate, isUpload ? FileAccess.Write : FileAccess.Read);
                ServerFileInfoClass record = new ServerFileInfoClass
                {
                    FStream = fs,
                    ServerPath = path,
                    Length = fif.Length,
                };
                int id = GenerateRandomFileStreamId(1 << 16);
                ServerFileSet.Add(id, record);
                SendBytes(client, new HB32Header { Flag = SocketDataFlag.DownloadAllowed ^ mask }, id.ToString());
            }
            catch (Exception ex)
            {
                SendBytes(client, new HB32Header { Flag = SocketDataFlag.DownloadDenied ^ mask }, ex.Message);
            }
        }

        /// <summary>
        /// 确定当前路径下文件是否被其他下载 Socket 占用
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private bool IsFileOccupying(string path)
        {
            lock (this.ServerFileSet)
            {
                List<int> ids = new List<int>(ServerFileSet.Keys);
                for (int i = 0; i < ids.Count; ++i)
                {
                    ServerFileInfoClass p = ServerFileSet[ids[i]];
                    if (p.ServerPath == path)
                    {
                        // 若该 FileStream 不在使用中 (60s空闲) 则释放
                        if ((DateTime.Now - p.LastTime).Seconds > 60)
                        {
                            p.FStream.Close();
                            ServerFileSet.Remove(ids[i]);
                            return false;
                        }
                        else
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
        }


        /// <summary>
        /// 生成新随机数用于 FileStreamId
        /// </summary>
        /// <returns></returns>
        private int GenerateRandomFileStreamId(int max)
        {
            lock (this.ServerFileSet)
            {
                Random rd = new Random();
                for (int id = rd.Next(0, max - 1); ; id = rd.Next(0, max - 1))
                {
                    bool match = false;
                    foreach (int setid in ServerFileSet.Keys)
                    {
                        if (setid == id)
                        {
                            match = true;
                            break;
                        }
                    }
                    if (!match) 
                    {
                        Display.TimeWriteLine("Created file id : " + id.ToString());
                        return id; 
                    }
                }
            }
        }

        private bool CheckKey(byte[] bytes)
        {
            return true;
        }



        public void Close()
        {
            server.Close();
        }
    }
}
