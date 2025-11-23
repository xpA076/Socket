using FileManager.Models.Serializable;
using FileManager.Models.Serializable.Crypto;
using FileManager.Models.Serializable.HeartBeat;
using FileManager.Models.Serializable.Transfer;
using FileManager.Models.SocketLib.Enums;
using FileManager.Models.SocketLib.SocketIO;
using FileManager.Utils.Bytes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.Models.SocketLib.SocketServer.Main
{
    public partial class SocketServer : SocketServerBase
    {
        const int PayloadStartIndex = 20;

        private void ReceiveData_Protocol(SocketResponder responder)
        {
            responder.SetTimeout(Config.SocketSendTimeOut, Config.SocketReceiveTimeOut);

            SocketSession session = null;

            while (this.flag_receive)
            {
                try
                {
                    byte[] bytes = responder.ReceiveBytes();
                    Guid guid = new Guid(bytes.AsSpan(0, 16));
                    responder.CurrentGuid = guid;
                    int idx = 16;
                    PacketType t = (PacketType)BytesParser.GetInt(bytes, ref idx);
                    switch (t)
                    {
                        case PacketType.Null:
                            throw new Exception("PacketType null");

                        case PacketType.KeyExchangeRequest:
                            KeyExchangeRequest keyExchangeRequest = KeyExchangeRequest.Build(bytes.AsSpan(PayloadStartIndex));
                            ResponseKeyExchange(responder, keyExchangeRequest);
                            break;

                        case PacketType.SessionRequest:
                            SessionRequest sessionRequest = SessionRequest.FromBytes(bytes, idx);
                            session = ResponseSession(responder, sessionRequest);
                            break;

                        case PacketType.DirectoryRequest:
                            DirectoryRequest directoryRequest = DirectoryRequest.FromBytes(bytes, idx);
                            ResponseDirectory(responder, directoryRequest, session);
                            break;

                        case PacketType.DownloadRequest:
                            DownloadRequest downloadRequest = DownloadRequest.FromBytes(bytes, idx);
                            ResponseDownloadFile(responder, downloadRequest, session);
                            break;

                        case PacketType.UploadRequest:
                            UploadRequest uploadRequest = UploadRequest.FromBytes(bytes, idx);
                            ResponseUploadFile(responder, uploadRequest, session);
                            break;

                        case PacketType.ReleaseFileRequest:
                            ReleaseFileRequest releaseFileRequest = ReleaseFileRequest.FromBytes(bytes, idx);
                            ReleaseFile(responder, releaseFileRequest, session);
                            break;

                        case PacketType.HeartBeatRequest:
                            HeartBeatRequest heartBeatRequest = HeartBeatRequest.FromBytes(bytes, idx);
                            ResponseHeartBeat(responder, heartBeatRequest);
                            break;

                        case PacketType.CustomizedPacketRequest:
                            byte[] bs = new byte[bytes.Length - 4];
                            Array.Copy(bytes, 4, bs, 0, bs.Length);
                            ResponseCustomizedPacket(responder, bs);
                            break;

                        case PacketType.DisconnectRequest:
                            DisposeClient(responder);
                            return;
                        default:
                            throw new Exception("Invalid socket header in receiving");
                    }
                }
                catch (SocketException ex)
                {
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

    }
}
