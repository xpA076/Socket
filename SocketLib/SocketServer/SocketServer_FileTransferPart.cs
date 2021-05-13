using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using SocketLib.Enums;

namespace SocketLib.SocketServer
{
    public partial class SocketServer : SocketServerBase
    {

        /// <summary>
        /// 小文件下载响应
        /// client : SocketPacketFlag.DownloadRequest + server 文件路径string -> (UTF-8)bytes
        /// server : SocketDataFlag.DownloadAllowed + 文件bytes
        ///     or : SocketPacketFlag.DownloadDenied + err_msg
        /// </summary>
        /// <param name="client"></param>
        /// <param name="bytes"></param>
        private void ResponseDownloadSmallFile(Socket client, byte[] bytes)
        {
            byte[] _bytes = new byte[1];
            string err_msg = "";
            try
            {
                if ((GetIdentity(client) & SocketIdentity.ReadFile) == 0)
                {
                    throw new Exception("Socket not authenticated.");
                }
                string path = Encoding.UTF8.GetString(bytes);
                _bytes = File.ReadAllBytes(path);
            }
            catch (Exception ex)
            {
                err_msg = ex.Message;
            }
            if (string.IsNullOrEmpty(err_msg))
            {
                SendBytes(client, new HB32Header { Flag = SocketPacketFlag.DownloadAllowed }, _bytes);
            }
            else
            {
                SendBytes(client, new HB32Header { Flag = SocketPacketFlag.DownloadDenied }, err_msg);
            }
                
        }

        /// <summary>
        /// 小文件上传响应
        /// client : SocketPacketFlag.UploadRequest + path string(BytesParser encoded), content bytes
        /// server : SocketPacketFlag.UploadAllowed + new byte[1]
        ///     or : SocketPacketFlag.UploadDenied + err_msg
        /// </summary>
        /// <param name="client"></param>
        /// <param name="bytes"></param>
        private void ResponseUploadSmallFile(Socket client, byte[] bytes)
        {
            string err_msg = "";
            try
            {
                if ((GetIdentity(client) & SocketIdentity.WriteFile) == 0)
                {
                    throw new Exception("Socket not authenticated.");
                }
                int pt = 0;
                string path = BytesConverter.ParseString(bytes, ref pt);
                byte[] contentBytes = new byte[bytes.Length - pt];
                Array.Copy(bytes, pt, contentBytes, 0, contentBytes.Length);
                File.WriteAllBytes(path, contentBytes);
            }
            catch (Exception ex)
            {
                err_msg = ex.Message;
            }
            if (string.IsNullOrEmpty(err_msg))
            {
                SendBytes(client, new HB32Header { Flag = SocketPacketFlag.UploadAllowed }, new byte[1]);
            }
            else
            {
                SendBytes(client, new HB32Header { Flag = SocketPacketFlag.UploadDenied }, err_msg);
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="client"></param>
        /// <param name="header"></param>
        /// <param name="bytes"></param>
        private void ResponseTransferPacket(Socket client, HB32Header header, byte[] bytes)
        {
            switch (header.Flag)
            {
                case SocketPacketFlag.UploadPacketRequest:
                    ResponseUploadPacket(client, header, bytes);
                    break;
                case SocketPacketFlag.DownloadPacketRequest:
                    ResponseDownloadPacket(client, header);
                    break;
            }
        }


        private void ResponseDownloadPacket(Socket client, HB32Header header)
        {
            byte[] responseBytes = new byte[1];
            string err_msg = "";
            int i1 = 0;
            try
            {
                if ((GetIdentity(client) & SocketIdentity.ReadFile) == 0)
                {
                    throw new Exception("Socket not authenticated.");
                }

                if (!ServerFileSet.ContainsKey(header.I1))
                {
                    i1 = 1;
                    throw new Exception("No fsid key in available server filestream");
                }

                SocketServerFileStreamInfo fs_info = ServerFileSet[header.I1];
                FileStream fs = fs_info.FStream;

                /// I2 == -1 则关闭 FileStream
                if (header.I2 == -1)
                {
                    fs.Close();
                    ServerFileSet.Remove(header.I1);
                    Log("Released file id : " + header.I1.ToString(), LogLevel.Info);
                    return;
                }

                /// 确定 Server 端 FileStream 读起点和 byte 长度
                long begin = (long)header.I2 * HB32Encoding.DataSize;
                int length = HB32Encoding.DataSize; /// <- 读/写 byte长度
                if (begin + HB32Encoding.DataSize > fs_info.Length)
                {
                    length = (int)(fs_info.Length - begin);
                }
                responseBytes = new byte[length];

                /// 读取 FileStream bytes
                lock (fs)
                {
                    fs.Seek(begin, SeekOrigin.Begin);
                    fs.Read(responseBytes, 0, length);
                }
                fs_info.LastTime = DateTime.Now;
            }
            catch (Exception ex)
            {
                err_msg = ex.Message;
                Log("ResponseDownloadPacket exception : " + err_msg, LogLevel.Warn);
            }

            if (string.IsNullOrEmpty(err_msg))
            {
                SendBytes(client, new HB32Header { Flag = SocketPacketFlag.DownloadPacketResponse }, responseBytes);
            }
            else
            {
                SendBytes(client, new HB32Header { Flag = SocketPacketFlag.DownloadDenied, I1 = i1 }, err_msg);
            }
        }


        private void ResponseUploadPacket(Socket client, HB32Header header, byte[] bytes)
        {
            int i1 = 0;
            string err_msg = "";
            try
            {
                if ((GetIdentity(client) & SocketIdentity.WriteFile) == 0)
                {
                    throw new Exception("Socket not authenticated.");
                }

                if (!ServerFileSet.ContainsKey(header.I1))
                {
                    i1 = 1;
                    throw new Exception("No fsid key in available server filestream");
                }

                SocketServerFileStreamInfo fs_info = ServerFileSet[header.I1];
                FileStream fs = fs_info.FStream;

                /// I2 == -1 则关闭 FileStream
                if (header.I2 == -1)
                {
                    fs.Close();
                    ServerFileSet.Remove(header.I1);
                    Log("Released file id : " + header.I1.ToString(), LogLevel.Info);
                    return;
                }

                /// FileStream 写入
                long begin = (long)header.I2 * HB32Encoding.DataSize;
                lock (fs)
                {
                    fs.Seek(begin, SeekOrigin.Begin);
                    fs.Write(bytes, 0, header.ValidByteLength);
                }
                fs_info.LastTime = DateTime.Now;
            }
            catch (Exception ex)
            {
                err_msg = ex.Message;
                Log("ResponseUploadPacket exception : " + err_msg, LogLevel.Warn);
            }
            if (string.IsNullOrEmpty(err_msg))
            {
                SendHeader(client, SocketPacketFlag.UploadPacketResponse);
            }
            else
            {
                SendHeader(client, SocketPacketFlag.UploadDenied, i1);
            }
        }


        /// <summary>
        /// 相应 client 端的 fsid 请求
        /// 文件占用 (文件活跃时间超时被自动清除) 时返回对应 fsid , 否则生成新fsid并返回
        /// 记录在 ServerFileSet 中
        /// </summary>
        /// <param name="client"></param>
        /// <param name="header">依据header判断上传/下载</param>
        /// <param name="bytes"></param>
        private void ResponseFileStreamId(Socket client, HB32Header header, byte[] bytes)
        {
            string err_msg = "";
            SocketPacketFlag mask = header.Flag & (SocketPacketFlag)(1 << 8);
            int fsid = -1;
            try
            {
                SocketIdentity required_identity = (mask > 0) ? SocketIdentity.WriteFile : SocketIdentity.ReadFile;
                if ((GetIdentity(client) & required_identity) == 0)
                {
                    throw new Exception("Socket not authenticated.");
                }

                string path = Encoding.UTF8.GetString(bytes);
                if (IsFileOccupying(path, out fsid)) {; }
                else
                {
                    FileInfo fif = new FileInfo(path);
                    FileStream fs = new FileStream(path, FileMode.OpenOrCreate, (mask > 0) ? FileAccess.Write : FileAccess.Read);
                    SocketServerFileStreamInfo record = new SocketServerFileStreamInfo
                    {
                        FStream = fs,
                        ServerPath = path,
                        Length = fif.Length,
                    };
                    fsid = GenerateRandomFileStreamId(1 << 16);
                    ServerFileSet.Add(fsid, record);
                }
            }
            catch (Exception ex)
            {
                err_msg = ex.Message;
            }
            if (string.IsNullOrEmpty(err_msg))
            {
                SendBytes(client, new HB32Header { Flag = SocketPacketFlag.DownloadAllowed | mask }, fsid.ToString());
            }
            else
            {
                SendBytes(client, new HB32Header { Flag = SocketPacketFlag.DownloadDenied | mask }, err_msg);
            }
        }

        
        /// <summary>
        /// 确定当前路径下文件是否被其他下载 Socket 占用
        /// return true 时, out fsid 表示当前FileStream 对应 fsid
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private bool IsFileOccupying(string path, out int fsid)
        {
            //  ***** todo ******* 区分Upload / Download
            // 21.05.01
            lock (this.ServerFileSet)
            {
                fsid = -1;
                List<int> ids = new List<int>(ServerFileSet.Keys);
                for (int i = 0; i < ids.Count; ++i)
                {
                    SocketServerFileStreamInfo p = ServerFileSet[ids[i]];
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
                            fsid = ids[i];
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
                        Log("Created file id : " + id.ToString(), LogLevel.Info);
                        return id;
                    }
                }
            }
        }

    }
}
