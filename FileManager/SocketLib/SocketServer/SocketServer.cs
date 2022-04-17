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
            HB32Packet f = HB32Packet.Null;
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
                            case HB32Packet.SessionRequest:
                                session = ResponseSession(responder, bytes);
                                break;

                            case HB32Packet.DirectoryRequest:
                                ResponseDirectory(responder, bytes, session);
                                break;

                            case HB32Packet.FileRequest:
                                ResponseFileRequest(responder, bytes, session);
                                break;


                            #region Download
                            case HB32Packet.DownloadRequest:
                                ResponseDownloadFile(responder, bytes, session);
                                //ResponseDownloadSmallFile(responder, bytes);
                                break;
                            case HB32Packet.DownloadFileStreamIdRequest:
                                ResponseFileStreamId(responder, header, bytes);
                                break;
                            case HB32Packet.DownloadPacketRequest:
                                ResponseTransferPacket(responder, header, bytes);
                                break;
                            #endregion

                            #region Upload
                            case HB32Packet.UploadRequest:
                                ResponseUploadSmallFile(responder, bytes);
                                break;
                            case HB32Packet.UploadFileStreamIdRequest:
                                ResponseFileStreamId(responder, header, bytes);
                                break;
                            case HB32Packet.UploadPacketRequest:
                                ResponseTransferPacket(responder, header, bytes);
                                break;
                            #endregion


                            case HB32Packet.DisconnectRequest:
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
