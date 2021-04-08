using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SocketLib.SocketServer
{
    public partial class SocketServer : SocketServerBase
    {

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
                SendBytes(client, new HB32Header { Flag = SocketDataFlag.DownloadDenied }, ex.Message);
                return;
            }
            SendBytes(client, new HB32Header { Flag = SocketDataFlag.DownloadAllowed }, _bytes);
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
                SendBytes(client, new HB32Header { Flag = SocketDataFlag.UploadDenied }, ex.Message);
                return;
            }
            SendBytes(client, new HB32Header { Flag = SocketDataFlag.UploadAllowed }, new byte[1]);
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
                if (header.Flag == SocketDataFlag.UploadPacketRequest)
                {
                    SendHeader(client, new HB32Header { Flag = SocketDataFlag.UploadDenied });
                }
                else
                {
                    SendBytes(client, new HB32Header { Flag = SocketDataFlag.DownloadDenied }, 
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
            lock (fs)
            {
                fs.Seek(begin, SeekOrigin.Begin);
                if (header.Flag == SocketDataFlag.UploadPacketRequest)
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
            fs_info.LastTime = DateTime.Now;

            /// response
            if (header.Flag == SocketDataFlag.UploadPacketRequest)
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
            SocketDataFlag upload_mask = (SocketDataFlag)((isUpload ? 1 : 0) << 8);
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
                    SendBytes(client, new HB32Header { Flag = SocketDataFlag.DownloadDenied ^ upload_mask }, "key error");
                    return;
                }
            }
            else
            {
                path = Encoding.UTF8.GetString(bytes);
            }

            /// 验证文件是否被占用
            if (IsFileOccupying(path, out int occupied_fsid))
            {
                //SendBytes(client, new HB32Header { Flag = SocketDataFlag.DownloadDenied ^ upload_mask }, "file occupied");
                SendBytes(client, new HB32Header { Flag = SocketDataFlag.DownloadAllowed ^ upload_mask }, occupied_fsid.ToString());
                return;
            }
            else
            {
                /// 生成 FileStreamId 并记录
                try
                {
                    FileInfo fif = new FileInfo(path);
                    FileStream fs = new FileStream(path, FileMode.OpenOrCreate, isUpload ? FileAccess.Write : FileAccess.Read);
                    SocketServerFileStreamInfo record = new SocketServerFileStreamInfo
                    {
                        FStream = fs,
                        ServerPath = path,
                        Length = fif.Length,
                    };
                    int id = GenerateRandomFileStreamId(1 << 16);
                    ServerFileSet.Add(id, record);
                    SendBytes(client, new HB32Header { Flag = SocketDataFlag.DownloadAllowed ^ upload_mask }, id.ToString());
                }
                catch (Exception ex)
                {
                    SendBytes(client, new HB32Header { Flag = SocketDataFlag.DownloadDenied ^ upload_mask }, ex.Message);
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
