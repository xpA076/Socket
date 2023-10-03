using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.SocketLib.Enums
{
    public enum PacketType : int
    {
        None = 0x0,
        Null = 0x0001,

        ExceptionFlag = 0x0080,


        KeyExchange = 0xFFAB,
        KeyEncrypted = 0xFFAC,


        /// Mode 0xA3 : Proxy
        /// 
        ProxyRouteRequest = 0xA301,
        ProxyResponse = 0xA310,
        ProxyException = 0xA390,


        /// Mode 0xA4 : ReversedProxy
        /// 
        ReversedProxyLongConnectionRequest = 0xA401,
        ReverserProxyLongConnectionQuery = 0xA402,
        ReversedProxyConnectionRequest = 0xA403,
        ReversedProxyResponse = 0xA410,
        ReversedProxyException = 0xA490,

        /// Mode 0xAC : Authentication
        /// 
        AuthenticationRequest = 0xAC01,
        AuthenticationResponse = 0xAC10,
        AuthenticationException = 0xAC90,

        /// <summary>
        /// Mode 0x20 : Session
        /// </summary>
        SessionRequest = 0x2001,
        SessionResponse = 0x2010,
        SetSessionRequest = 0x2002,
        SetSessionResponse = 0x2020,



        /// Mode 0x01 : Directory query
        DirectoryRequest = 0x0101,
        DirectoryResponse = 0x0110,
        DirectoryException = 0x0190,
        DirectorySizeRequest = 0x0102,
        DirectorySizeResponse = 0x0120,
        DirectoryCheck = 0x0103,


        /// Mode 0x02 : Download
        /// Request bits: 
        ///     0x1 - request
        ///     0x2 - FileStreamId request
        ///     0x3 - package request
        /// Response bits:
        ///     0x1 - allowed
        ///     0x2 - packet response
        ///     0x9 - denied
        DownloadRequest = 0x0201,
        DownloadResponse = 0x0210,
        DownloadAllowed = 0x0220,
        DownloadDenied = 0x0290,
        DownloadFileStreamIdRequest = 0x0202,
        DownloadPacketRequest = 0x010203, // 发往server的包不含byte[]数据
        DownloadPacketResponse = 0x0220,

        /// Mode 0x03 : Upload
        /// Request bits: 
        ///     0x1 - request
        ///     0x2 - FileStreamId request
        ///     0x3 - package request
        /// Response bits:
        ///     0x1 - allowed
        ///     0x2 - packet response
        ///     0x9 - denied
        UploadRequest = 0x0301,
        UploadResponse = 0x0310,
        UploadDenied = 0x0390,
        UploadFileStreamIdRequest = 0x0302,
        UploadPacketRequest = 0x0303,
        UploadPacketResponse = 0x010320, // 发往client的包不含byte[]数据

        /// Mode 0x04 : Create directory
        CreateDirectoryRequest = 0x0401,
        CreateDirectoryAllowed = 0x0410,
        CreateDirectoryDenied = 0x0490,


        /// Mode 0x05 : Delete
        DeleteRequest,
        DeleteAllowed,
        DeleteDenied,

        /// Mode 0x06 : Release file
        ReleaseFileRequest = 0x0601,
        ReleaseFileResponse = 0x0610,


        /// Mode 0x07 : HeartBeat
        HeartBeatRequest = 0x0701,
        HeartBeatResponse = 0x0710,




        /// Mode 0x10 : Transfer
        /// 
        TransferRequest = 0x1001,
        TransferResponse = 0x1010,
        TransferException = 0x1090,

        FileRequest = 0x1101,
        FileResponse = 0x1110,
        FileException = 0x1190,

        /// Mode 0x30 : Current handle for customized packet
        CustomizedPacketRequest = 0x3001,
        CustomizedPacketResponse = 0x3010,


        RemoteRunRequest,
        RemoteRunAllowed,
        RemoteRunDenied,

        StreamRequest,
        StreamResponse,

        DisconnectRequest = 0x110000,

        
    };
}