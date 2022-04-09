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
using FileManager.Models.Serializable;
using FileManager.SocketLib.Enums;


namespace FileManager.SocketLib.SocketServer
{

    public partial class SocketServer : SocketServerBase
    {
        public event SocketIdentityCheckEventHandler CheckIdentity;

        public SocketServerConfig Config { get; set; } = new SocketServerConfig();

        protected SocketServer()
        {

        }

        public SocketServer(IPAddress ip):base(ip)
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
        protected override void ReceiveData(object responderObject)
        {
            SocketResponder responder = responderObject as SocketResponder;
            responder.SetTimeout(Config.SocketSendTimeOut, Config.SocketReceiveTimeOut);
            SocketSession session = null;
            /// Server 数据响应主循环
            SocketPacketFlag f = SocketPacketFlag.Null;
            try
            {
                int error_count = 0;
                while (flag_receive & error_count < 5)
                {
                    try
                    {
                        responder.ReceiveBytes(out HB32Header header, out byte[] bytes);
                        f = header.Flag;
                        switch (header.Flag)
                        {
                            case SocketPacketFlag.SessionRequest:
                                session = ResponseSession(responder, bytes);
                                break;

                            case SocketPacketFlag.DirectoryRequest:
                                ResponseDirectory(responder, bytes);
                                break;
                            case SocketPacketFlag.DirectorySizeRequest:
                                ResponseDirectorySize(responder, bytes);
                                break;

                            case SocketPacketFlag.DirectoryCheck:
                                ResponseDirectoryCheck(responder, bytes);
                                break;

                            case SocketPacketFlag.CreateDirectoryRequest:
                                ResponseCreateDirectory(responder, bytes);
                                break;

                            #region Download
                            case SocketPacketFlag.DownloadRequest:
                                ResponseDownloadFile(responder, bytes);
                                //ResponseDownloadSmallFile(responder, bytes);
                                break;
                            case SocketPacketFlag.DownloadFileStreamIdRequest:
                                ResponseFileStreamId(responder, header, bytes);
                                break;
                            case SocketPacketFlag.DownloadPacketRequest:
                                ResponseTransferPacket(responder, header, bytes);
                                break;
                            #endregion

                            #region Upload
                            case SocketPacketFlag.UploadRequest:
                                ResponseUploadSmallFile(responder, bytes);
                                break;
                            case SocketPacketFlag.UploadFileStreamIdRequest:
                                ResponseFileStreamId(responder, header, bytes);
                                break;
                            case SocketPacketFlag.UploadPacketRequest:
                                ResponseTransferPacket(responder, header, bytes);
                                break;
                            #endregion


                            case SocketPacketFlag.DisconnectRequest:
                                DisposeClient(responder);
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
                                DisposeClient(responder);
                                Log("Connection closed (client closed). " + ex.Message, LogLevel.Info);
                                return;
                            // Socket 超时
                            case 10060:
                                Thread.Sleep(200);
                                Log("Socket timeout. " + ex.Message, LogLevel.Trace);
                                continue;
                            default:
                                Log("Server receive data :" + ex.Message, LogLevel.Warn);
                                continue;
                        }
                    }
                    catch (Exception ex)
                    {
                        error_count++;
                        if (ex.Message.Contains("Buffer receive error: cannot receive package"))
                        {
                            DisposeClient(responder);
                            Log(ex.Message, LogLevel.Trace);
                            return;
                        }
                        if (ex.Message.Contains("Invalid socket header"))
                        {
                            DisposeClient(responder);
                            Log("Connection closed : " + ex.Message, LogLevel.Warn);
                            return;
                        }
                        Log("Server exception :" + ex.Message, LogLevel.Warn);
                        Thread.Sleep(200);
                        continue;
                    }
                }
                Log("Connection closed.", LogLevel.Warn);
            }
            catch (Exception ex)
            {
                //Log("Identity authentication exception :" + ex.Message, LogLevel.Error);
                Log("Unexcepted exception in server [" + f.ToString() + "] : " + ex.Message, LogLevel.Error);
            }
        }

        #region Session


        /// <summary>
        /// 在获取 Responder 后, 向 client 端的 session 请求做出响应
        /// </summary>
        /// <param name="responder"></param>
        /// <returns> server 端新建立的或查找到的 session 对象, 不成功返回 null </returns>
        private SocketSession ResponseSession(SocketResponder responder, byte[] recv_bytes)
        {
            SessionRequest request = SessionRequest.FromBytes(recv_bytes);
            if (request.Type == SessionRequest.BytesType.KeyBytes)
            {
                SessionsLock.EnterWriteLock();
                try
                {
                    SocketSession ss = CreateSession(request.Bytes);
                    Sessions.Add(ss.BytesInfo.Index, ss);
                    SessionResponse response = new SessionResponse()
                    {
                        Type = SessionResponse.ResponseType.NewSessionBytes,
                        Bytes = ss.BytesInfo.ToBytes()
                    };
                    responder.SendBytes(SocketPacketFlag.SessionResponse, response.ToBytes());
                    return ss;
                }
                finally
                {
                    SessionsLock.ExitWriteLock();
                }
            }
            else if (request.Type == SessionRequest.BytesType.SessionBytes)
            {
                SessionsLock.EnterReadLock();
                try
                {
                    SessionBytesInfo sessionBytesInfo = SessionBytesInfo.FromBytes(recv_bytes);
                    if (Sessions.ContainsKey(sessionBytesInfo.Index))
                    {
                        SocketSession ss = Sessions[sessionBytesInfo.Index];
                        if (sessionBytesInfo.Equals(ss.BytesInfo))
                        {
                            /// SessionBytes 校验通过
                            SessionResponse response = new SessionResponse()
                            {
                                Type = SessionResponse.ResponseType.NoModify,
                                Bytes = new byte[0]
                            };
                            responder.SendBytes(SocketPacketFlag.SessionResponse, response.ToBytes());
                            return ss;
                        }
                        else
                        {
                            /// client 和 server 端内容不一致
                            throw new SocketSessionException("Content mismatch");
                        }
                    }
                    else
                    {
                        /// client 和 server 端内容不一致, index 不存在
                        throw new SocketSessionException("Invalid index");
                    }
                }
                catch (SocketSessionException ex)
                {
                    SessionResponse response = new SessionResponse()
                    {
                        Type = SessionResponse.ResponseType.SessionException,
                        Bytes = Encoding.UTF8.GetBytes(ex.Message)
                    };
                    responder.SendBytes(SocketPacketFlag.SessionResponse, response.ToBytes());
                    return null;
                }
                finally
                {
                    SessionsLock.ExitReadLock();
                }
            }
            else
            {
                return null;
            }
        }



        private SocketSession CreateSession(byte[] key_bytes)
        {
            /// SessionBytesInfo
            SessionBytesInfo bytes_info = new SessionBytesInfo();
            bytes_info.Identity = GetIdentity(key_bytes);
            Random rd = new Random();
            for (int sid = rd.Next(1, 2 << 30 - 1); ; sid = rd.Next(1, 2 << 30 - 1))
            {
                if (Sessions.ContainsKey(sid))
                {
                    continue;
                }
                else
                {
                    bytes_info.Index = sid;
                    break;
                }
            }
            rd.NextBytes(bytes_info.VerificationBytes);

            /// SocketSession
            SocketSession ss = new SocketSession();
            ss.BytesInfo = bytes_info;
            return ss;
        }





        private SocketIdentity GetIdentity(byte[] KeyBytes)
        {
            return SocketIdentity.All;
        }


        #endregion






        private void DisposeClient(SocketResponder responder)
        {
            try
            {
                responder.Close();
            }
            catch (Exception) { }
            finally
            {
                //ClientSessions.Remove(responder);
            }
        }



    }
}
