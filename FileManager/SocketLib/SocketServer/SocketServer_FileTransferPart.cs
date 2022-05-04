﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FileManager.Exceptions.Server;
using FileManager.Models.Serializable;
using FileManager.SocketLib.Enums;
using FileManager.SocketLib.SocketServer.Models;

namespace FileManager.SocketLib.SocketServer
{
    public partial class SocketServer : SocketServerBase
    {
        private void ResponseDownloadFile(SocketResponder responder, byte[] bytes, SocketSession session)
        {
            try
            {
                if (!session.AllowReadFile())
                {
                    throw new SocketAuthenticationException();
                }
                DownloadRequest request = DownloadRequest.FromBytes(bytes);
                FileResource resource;
                /// 获取 FileResource
                if (request.Type == DownloadRequest.RequestType.QueryByPath)
                {
                    string server_path = PathTranslator.ToTruePath(request.ViewPath, session);
                    resource = FileResourceManager.GetResource(server_path, FileAccess.Read);
                }
                else
                {
                    throw new ServerInternalException("not implemented");
                }
                byte[] read_bytes = resource.ReadSpan(request.Begin, request.Length);
                DownloadResponse response = new DownloadResponse(read_bytes);
                responder.SendBytes(HB32Packet.DownloadResponse, response.ToBytes());
            }
            catch (SocketAuthenticationException)
            {
                string err_msg = "Authentication exception";
                DownloadResponse response = new DownloadResponse(err_msg);
                responder.SendBytes(HB32Packet.DownloadResponse, response.ToBytes());
            }
            catch (ServerInternalException ex)
            {
                string err_msg = "Download response exception from server: " + ex.Message;
                DownloadResponse response = new DownloadResponse(err_msg);
                responder.SendBytes(HB32Packet.DownloadResponse, response.ToBytes());
            }

        }


        private void ResponseUploadFile(SocketResponder responder, byte[] bytes, SocketSession session)
        {
            try
            {
                if (!session.AllowWriteFile())
                {
                    throw new SocketAuthenticationException();
                }
                UploadRequest request = UploadRequest.FromBytes(bytes);
                FileResource resource;
                if (request.Type == UploadRequest.RequestType.ByPath)
                {
                    string server_path = PathTranslator.ToTruePath(request.ViewPath, session);
                    resource = FileResourceManager.GetResource(server_path, FileAccess.Write);
                }
                else
                {
                    throw new ServerInternalException("not implemented");
                }
                resource.WriteSpan(request.Begin, request.Bytes);
                UploadResponse response = UploadResponse.BuildSuccessResponse();
                responder.SendBytes(HB32Packet.UploadResponse, response.ToBytes());
            }
            catch (SocketAuthenticationException)
            {
                string err_msg = "Authentication exception";
                UploadResponse response = new UploadResponse(err_msg);
                responder.SendBytes(HB32Packet.UploadResponse, response.ToBytes());
            }
            catch (ServerInternalException ex)
            {
                string err_msg = "Upload response exception from server: " + ex.Message;
                UploadResponse response = new UploadResponse(err_msg);
                responder.SendBytes(HB32Packet.UploadResponse, response.ToBytes());
            }
        }



        /*
        /// <summary>
        /// 小文件下载响应
        /// client : SocketPacketFlag.DownloadRequest + server 文件路径string -> (UTF-8)bytes
        /// server : SocketDataFlag.DownloadAllowed + 文件bytes
        ///     or : SocketPacketFlag.DownloadDenied + err_msg
        /// </summary>
        /// <param name="client"></param>
        /// <param name="bytes"></param>
        private void ResponseDownloadSmallFile(SocketResponder responder, byte[] bytes)
        {
            
            byte[] _bytes = new byte[1];
            string err_msg = "";
            try
            {
                if ((GetIdentity(responder) & SocketIdentity.ReadFile) == 0)
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
                responder.SendBytes(SocketPacketFlag.DownloadAllowed, _bytes);
            }
            else
            {
                responder.SendBytes(SocketPacketFlag.DownloadDenied, err_msg);
            }
                
        }
        */

        /// <summary>
        /// 小文件上传响应
        /// client : SocketPacketFlag.UploadRequest + path string(BytesParser encoded), content bytes
        /// server : SocketPacketFlag.UploadAllowed + new byte[1]
        ///     or : SocketPacketFlag.UploadDenied + err_msg
        /// </summary>
        /// <param name="client"></param>
        /// <param name="bytes"></param>
        private void ResponseUploadSmallFile(SocketResponder responder, byte[] bytes)
        {
            string err_msg = "";
            try
            {
                if (true)
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
                responder.SendBytes(HB32Packet.UploadResponse, new byte[1]);
            }
            else
            {
                responder.SendBytes(HB32Packet.UploadDenied, err_msg);
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="client"></param>
        /// <param name="header"></param>
        /// <param name="bytes"></param>
        private void ResponseTransferPacket(SocketResponder responder, HB32Header header, byte[] bytes)
        {
            switch (header.Flag)
            {
                case HB32Packet.UploadPacketRequest:
                    ResponseUploadPacket(responder, header, bytes);
                    break;
                case HB32Packet.DownloadPacketRequest:
                    ResponseDownloadPacket(responder, header);
                    break;
            }
        }


        private void ResponseDownloadPacket(SocketResponder responder, HB32Header header)
        {
            byte[] responseBytes = new byte[1];
            string err_msg = "";
            /// 若 server 端字典中不含该fsid (对应server重启), 将i1置1并返回至client端
            int i1 = 0;
            try
            {
                if (true)
                {
                    throw new Exception("Socket not authenticated.");
                }

                if (!ServerFileSet.ContainsKey(header.I1))
                {
                    i1 = 1;
                    throw new Exception("No fsid key : [" + header.I1.ToString() + "] in available server filestream");
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
                responder.SendBytes(HB32Packet.DownloadPacketResponse, responseBytes);
            }
            else
            {
                responder.SendBytes(HB32Packet.DownloadDenied, err_msg, i1: i1);
            }
        }


        private void ResponseUploadPacket(SocketResponder responder, HB32Header header, byte[] bytes)
        {
            int i1 = 0;
            string err_msg = "";
            try
            {
                if (true)
                {
                    throw new Exception("Socket not authenticated.");
                }

                if (!ServerFileSet.ContainsKey(header.I1))
                {
                    i1 = 1;
                    throw new Exception("No fsid key : [" + header.I1.ToString() + "] in available server filestream");
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
                responder.SendHeader(HB32Packet.UploadPacketResponse);
            }
            else
            {
                responder.SendHeader(HB32Packet.UploadDenied, i1: i1);
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
        private void ResponseFileStreamId(SocketResponder responder, HB32Header header, byte[] bytes)
        {
            string err_msg = "";
            HB32Packet mask = header.Flag & (HB32Packet)(1 << 8);
            int fsid = -1;
            try
            {
                SocketIdentity required_identity = (mask > 0) ? SocketIdentity.WriteFile : SocketIdentity.ReadFile;
                if (true)
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
                responder.SendBytes(HB32Packet.DownloadAllowed | mask, fsid.ToString());
            }
            else
            {
                responder.SendBytes(HB32Packet.DownloadDenied | mask, err_msg);
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
