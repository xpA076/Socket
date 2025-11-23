using FileManager.Models.SocketLib.Enums;
using FileManager.Models.SocketLib.SocketIO;
using FileManager.Utils.Bytes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FileManager.Models.Serializable;

namespace FileManager.Models.SocketLib.SocketServer.Main
{
    public partial class SocketServer : SocketServerBase
    {
        private void Response(SocketResponder responder, ISocketSerializable response)
        {
            BytesBuilder bb = new BytesBuilder();
            switch (response.GetType().Name)
            {
                case "KeyExchangeResponse":
                    bb.Append((int)PacketType.KeyExchangeResponse);
                    break;
                case "SessionResponse":
                    bb.Append((int)PacketType.SessionResponse);
                    break;
                case "DirectoryResponse":
                    bb.Append((int)PacketType.DirectoryResponse);
                    break;
                case "DownloadResponse":
                    bb.Append((int)PacketType.DownloadResponse);
                    break;
                case "UploadResponse":
                    bb.Append((int)PacketType.UploadResponse);
                    break;
                case "ReleaseFileResponse":
                    bb.Append((int)PacketType.ReleaseFileResponse);
                    break;
                case "HeartBeatResponse":
                    bb.Append((int)PacketType.HeartBeatResponse);
                    break;
            }
            bb.Concatenate(response.ToBytes());
            //responder.SendBytes(bb.GetBytes(), encryptText: encryptText);
        }


        private void Response(SocketResponder responder, PacketType packetType, ISocketSerializable response, bool encrypt = true)
        {
            BytesBuilder bb = new BytesBuilder();
            bb.Append(responder.CurrentGuid);
            bb.Append((int)packetType);
            bb.Concatenate(response.ToBytes());
            responder.SendBytes(bb.GetBytes(), encrypt);
        }
    }
}
