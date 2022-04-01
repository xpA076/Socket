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

        private readonly Dictionary<SocketResponder, SocketSessionInfo> ClientSessions = new Dictionary<SocketResponder, SocketSessionInfo>();

        private readonly Dictionary<string, SocketSessionInfo> Sessions = new Dictionary<string, SocketSessionInfo>();

        private readonly ReaderWriterLockSlim SessionLock = new ReaderWriterLockSlim();

        /// <summary>
        /// 在 Socket.accept() 获取到的 client 在这里处理
        /// 这个函数为 client 的整个生存周期
        /// </summary>
        /// <param name="responderObject">client socket</param>
        protected override void ReceiveData(object responderObject)
        {
            SocketResponder responder = responderObject as SocketResponder;
            responder.SetTimeout(Config.SocketSendTimeOut, Config.SocketReceiveTimeOut);
            /// 确认当前连接权限
            try
            {
                /// 接收 KeyBytes, 调用 CheckIdentity();
                /// 向 Dictionary 添加 SocketResponder 权限;
                /// 并向 Client 端返回 SocketIdentity
                responder.ReceiveBytes(out HB32Header header, out byte[] bytes);
                SocketSessionInfo session;
                string sessid;
                if (bytes[0] == 0)
                {
                    SocketIdentityCheckEventArgs e = new SocketIdentityCheckEventArgs(header, bytes);
                    CheckIdentity(this, e);
                    sessid = CreateNewSession();
                    session = GetSession(sessid);
                    session.Identity = e.CheckedIndentity;
                }
                else
                {
                    sessid = ParseSessionId(bytes);
                    session = GetSession(sessid);
                }

                lock (ClientSessions)
                {
                    ClientSessions.Add(responder, session);
                }
                responder.SendBytes(SocketPacketFlag.AuthenticationResponse, sessid, i1: (int)session.Identity);

            }
            catch (Exception ex)
            {
                Log("Response identity error : " + ex.Message, LogLevel.Error);
                ClientSessions.Remove(responder);
                return;
            }
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
                                ResponseDownloadSmallFile(responder, bytes);
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
            ClientSessions.Remove(responder);
        }

        #region Session

        
        private string ParseSessionId(byte[] bytes)
        {
            if (bytes[0] == 0)
            {
                return null;
            }
            else
            {
                int len = (int)bytes[0];
                return Encoding.UTF8.GetString(bytes.Skip(1).Take(len).ToArray());
            }
        }


        private SocketSessionInfo GetSession(string sessid)
        {
            try
            {
                SessionLock.EnterReadLock();
                return Sessions[sessid];
            }
            catch (Exception)
            {
                return null;
            }
            finally
            {
                SessionLock.ExitReadLock();
            }
        }


        /// <summary>
        /// 创建新session
        /// </summary>
        /// <returns> SessionId 字符串 </returns>
        private string CreateNewSession()
        {
            try
            {
                SocketSessionInfo session = new SocketSessionInfo();
                SessionLock.EnterWriteLock();
                Random rd = new Random();
                for (int sid = rd.Next(1, 2 << 30 - 1); ; sid = rd.Next(1, 2 << 30 - 1))
                {
                    string sessid = sid.ToString();
                    if (Sessions.ContainsKey(sessid))
                    {
                        continue;
                    }
                    else
                    {
                        Sessions.Add(sessid, session);
                        return sessid;
                    }
                }
            }
            finally
            {
                SessionLock.ExitWriteLock();
            }
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
                ClientSessions.Remove(responder);
            }
        }



        private SocketIdentity GetIdentity(SocketResponder responder)
        {
            lock (ClientSessions)
            {
                try
                {
                    return ClientSessions[responder].Identity;
                }
                catch (Exception)
                {
                    return SocketIdentity.None;
                }
            }
        }

    }
}
