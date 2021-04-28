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
                if ((GetIdentity(client) | SocketIdentity.ReadFile) == 0)
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
                int pt = 0;
                string path = BytesParser.ParseString(bytes, ref pt);
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
        /// <param name="isUpload"></param>
        private void ResponseTransferPacket(Socket client, HB32Header header, byte[] bytes)
        {
            /// header.Flag 为 SocketDataFlag.UploadPacketRequest 或 SocketDataFlag.DownloadPacketRequest
            if (!ServerFileSet.ContainsKey(header.I1))
            {
                if (header.Flag == SocketPacketFlag.UploadPacketRequest)
                {
                    SendHeader(client, new HB32Header { Flag = SocketPacketFlag.UploadDenied });
                }
                else
                {
                    SendBytes(client, new HB32Header { Flag = SocketPacketFlag.DownloadDenied }, 
                        Encoding.UTF8.GetBytes("no fsid key in available server filestream"));
                }
            }

            SocketServerFileStreamInfo fs_info = ServerFileSet[header.I1];
            FileStream fs = fs_info.FStream;
            /// I2 == -1 则关闭 FileStream
            /// 
            if (header.I2 == -1)
            {
                fs.Close();
                ServerFileSet.Remove(header.I1);
                Log("Released file id : " + header.I1.ToString(), LogLevel.Info);
                return;
            }

            /// 确定 Server 端 FileStream 读/写 起点和 byte 长度
            long begin = (long)header.I2 * HB32Encoding.DataSize;
            int length = HB32Encoding.DataSize; /// <- 读/写 byte长度
            if (begin + HB32Encoding.DataSize > fs_info.Length)
            {
                length = (int)(fs_info.Length - begin);
            }
            byte[] responseBytes = new byte[0];

            /// 定位 FileStream 读取/写入 bytes
            /// 
            if (header.Flag == SocketPacketFlag.UploadPacketRequest)
            {
                lock (fs)
                {
                    fs.Seek(begin, SeekOrigin.Begin);
                    fs.Write(bytes, 0, header.ValidByteLength);
                }
            }
            else
            {
                lock (fs)
                {
                    fs.Seek(begin, SeekOrigin.Begin);
                    responseBytes = new byte[length];
                    fs.Read(responseBytes, 0, length);
                }
            }

            fs_info.LastTime = DateTime.Now;

            /// response
            if (header.Flag == SocketPacketFlag.UploadPacketRequest)
            {
                SendHeader(client, new HB32Header { Flag = SocketPacketFlag.UploadPacketResponse });
            }
            else
            {
                SendBytes(client, new HB32Header { Flag = SocketPacketFlag.DownloadPacketResponse }, responseBytes);
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="client"></param>
        /// <param name="header"></param>
        /// <param name="bytes"></param>
        /// <param name="transferType"></param>
        private void ResponseFileStreamId(Socket client, HB32Header header, byte[] bytes, TransferType transferType)
        {
            SocketPacketFlag upload_mask = (SocketPacketFlag)((transferType == TransferType.Upload ? 1 : 0) << 8);
            string path = Encoding.UTF8.GetString(bytes);

            /// 验证文件是否被占用
            if (IsFileOccupying(path, out int occupied_fsid))
            {
                SendBytes(client, new HB32Header { Flag = SocketPacketFlag.DownloadAllowed ^ upload_mask }, occupied_fsid.ToString());
                return;
            }
            else
            {
                /// 生成 FileStreamId 并记录
                try
                {
                    FileInfo fif = new FileInfo(path);
                    FileStream fs = new FileStream(path, FileMode.OpenOrCreate, transferType == TransferType.Upload ? FileAccess.Write : FileAccess.Read);
                    SocketServerFileStreamInfo record = new SocketServerFileStreamInfo
                    {
                        FStream = fs,
                        ServerPath = path,
                        Length = fif.Length,
                    };
                    int id = GenerateRandomFileStreamId(1 << 16);
                    ServerFileSet.Add(id, record);
                    SendBytes(client, new HB32Header { Flag = SocketPacketFlag.DownloadAllowed ^ upload_mask }, id.ToString());
                }
                catch (Exception ex)
                {
                    SendBytes(client, new HB32Header { Flag = SocketPacketFlag.DownloadDenied ^ upload_mask }, ex.Message);
                }
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
