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

        private readonly Dictionary<SocketResponder, SocketIdentity> ClientIdentities = new Dictionary<SocketResponder, SocketIdentity>();
        private readonly object ClientIdentitiesLock = new object();


        /// <summary>
        /// 在 Socket.accept() 获取到的 client 在这里处理
        /// 这个函数为 client 的整个生存周期
        /// </summary>
        /// <param name="responderObject">client socket</param>
        protected override void ReceiveData(object responderObject)
        {
            SocketResponder responder = responderObject as SocketResponder;
            responder.SetTimeout(Config.SocketSendTimeOut, Config.SocketReceiveTimeOut);
            SocketPacketFlag f = SocketPacketFlag.Null;

            ResponseIdentity(responder);
            try
            {
                try
                {
                    //ResponseIdentity(responder);
                }
                catch(Exception ex)
                {
                    Log("Response identity error : " + ex.Message, LogLevel.Error);
                    ClientIdentities.Remove(responder);
                    return;
                }
                
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

                            case SocketPacketFlag.CreateDirectoryRequest:
                                ResponseCreateDirectory(responder, bytes);
                                break;

                            #region download
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

                            #region upload
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
                ClientIdentities.Remove(responder);
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
                ClientIdentities.Remove(responder);
            }
        }


        /// <summary>
        /// 应在 ReceiveData() 最开始调用
        /// 接收 KeyBytes, 调用 CheckIdentity();
        /// 向 Dictionary 添加 SocketResponder 权限;
        /// 并向 Client 端返回 SocketIdentity
        /// </summary>
        /// <param name="responder"></param>
        private void ResponseIdentity(SocketResponder responder)
        {
            responder.ReceiveBytes(out HB32Header header, out byte[] bytes);
            // Log("Received AuthenticationRequest", LogLevel.Info);
            SocketIdentityCheckEventArgs e = new SocketIdentityCheckEventArgs(header, bytes);
            CheckIdentity(this, e);
            SocketIdentity identity = e.CheckedIndentity;
            lock (ClientIdentitiesLock)
            {
                ClientIdentities.Add(responder, identity);
            }
            responder.SendHeader(SocketPacketFlag.AuthenticationResponse, i1: (int)identity);
        }


        private SocketIdentity GetIdentity(SocketResponder responder)
        {
            lock (ClientIdentitiesLock)
            {
                try
                {
                    return ClientIdentities[responder];
                }
                catch (Exception)
                {
                    return SocketIdentity.None;
                }
            }
        }

    }
}
