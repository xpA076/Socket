using FileManager.Models.Serializable;
using FileManager.SocketLib.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FileManager.SocketLib.SocketServer.Main
{
    public partial class SocketServer : SocketServerBase
    {
        private void ReceiveData_HB32(object responderObject)
        {
            SocketResponder responder = responderObject as SocketResponder;
            responder.SetTimeout(Config.SocketSendTimeOut, Config.SocketReceiveTimeOut);
            SocketSession session = null;
            /// Server 数据响应主循环
            PacketType f = PacketType.Null;
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
                            case PacketType.Null:
                                ResponeNothing(responder, bytes);
                                break;

                            case PacketType.SessionRequest:
                                SessionRequest request = SessionRequest.FromBytes(bytes);
                                session = ResponseSession(responder, request);
                                break;
                            case PacketType.DirectoryRequest:
                                DirectoryRequest directoryRequest = DirectoryRequest.FromBytes(bytes);
                                ResponseDirectory(responder, directoryRequest, session);
                                break;

                            #region Download

                            case PacketType.DownloadFileStreamIdRequest:
                                ResponseFileStreamId(responder, header, bytes);
                                break;
                            case PacketType.DownloadPacketRequest:
                                ResponseTransferPacket(responder, header, bytes);
                                break;
                            #endregion

                            #region Upload
                            case PacketType.UploadFileStreamIdRequest:
                                ResponseFileStreamId(responder, header, bytes);
                                break;
                            case PacketType.UploadPacketRequest:
                                ResponseTransferPacket(responder, header, bytes);
                                break;
                            #endregion

                            case PacketType.CustomizedPacketRequest:
                                ResponseCustomizedPacket(responder, bytes);
                                break;

                            case PacketType.DisconnectRequest:
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
    }
}
