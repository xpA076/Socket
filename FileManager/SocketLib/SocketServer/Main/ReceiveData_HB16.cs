using FileManager.Models.Serializable;
using FileManager.Models.Serializable.Crypto;
using FileManager.Models.Serializable.HeartBeat;
using FileManager.SocketLib.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FileManager.SocketLib.SocketServer.Main
{
    public partial class SocketServer : SocketServerBase
    {
        private void ReceiveData_HB16(object responderObject)
        {
            SocketResponder responder = responderObject as SocketResponder;
            responder.SetTimeout(Config.SocketSendTimeOut, Config.SocketReceiveTimeOut);
            SocketSession session = null;
            //this.ServerExchangeKeys(responder, out session);

            /// Server 数据响应主循环
            int error_count = 0;
            while (flag_receive & error_count < 5)
            {
                try
                {
                    byte[] bytes = responder.ReceiveBytes();
                    int idx = 0;
                    PacketType t = (PacketType)BytesParser.GetInt(bytes, ref idx);
                    switch (t)
                    {
                        case PacketType.Null:
                            throw new Exception("PacketType null");

                        case PacketType.KeyExchangeRequest:
                            KeyExchangeRequest keyExchangeRequest = KeyExchangeRequest.FromBytes(bytes, idx);
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



        private void ResponseKeyExchange(SocketResponder responder, KeyExchangeRequest request)
        {
            KeyExchangeResponse response = new KeyExchangeResponse();
            using (ECDiffieHellmanCng ec_server = new ECDiffieHellmanCng())
            {
                ec_server.KeyDerivationFunction = ECDiffieHellmanKeyDerivationFunction.Hash;
                ec_server.HashAlgorithm = CngAlgorithm.Sha256;
                CngKey clientKey = CngKey.Import(request.EcdhPublicKey, CngKeyBlobFormat.EccPublicBlob);
                byte[] sharedKey = ec_server.DeriveKeyMaterial(clientKey);
                responder.SetSymmetricKeys(sharedKey);
                response.EcdhPublicKey = ec_server.PublicKey.ToByteArray();
            }
            this.Response(responder, response, encryptText: false);
        }



        private void ServerExchangeKeys(SocketResponder responder, out SocketSession session)
        {
            /// Check identity
            session = CreateSession(null); // todo

            /// Response key exchange
            using(ECDiffieHellmanCng ec_server = new ECDiffieHellmanCng())
            {
                ec_server.KeyDerivationFunction = ECDiffieHellmanKeyDerivationFunction.Hash;
                ec_server.HashAlgorithm = CngAlgorithm.Sha256;
                byte[] recv_bytes = responder.ReceiveBytes();
                KeyExchangeRequest request = KeyExchangeRequest.FromBytes(recv_bytes);
                CngKey clientKey = CngKey.Import(request.EcdhPublicKey, CngKeyBlobFormat.EccPublicBlob);
                byte[] sharedKey = ec_server.DeriveKeyMaterial(clientKey);
                responder.SetSymmetricKeys(sharedKey);
                KeyExchangeResponse response = new KeyExchangeResponse()
                {
                    EcdhPublicKey = ec_server.PublicKey.ToByteArray()
                };
                responder.SendBytes(response.ToBytes(), encryptText: false);
            }

        }

    }
}
