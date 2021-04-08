using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketLib
{
    public enum SocketDataFlag : int
    {
        None = 0x0,

        /// Mode 0x01 : Directory query
        DirectoryRequest = 0x0101,
        DirectoryResponse = 0x0110,
        DirectoryException = 0x0190,
        DirectorySizeRequest = 0x0102,
        DirectorySizeResponse = 0x0120,


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
        DownloadAllowed = 0x0210,
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
        UploadAllowed = 0x0310,
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

        /// Mode 0x06 : Status report
        StatusReport = 0x0601,
        StatusQuery = 0x0602,


        /// Mode 0x10 : 
        /// 
        /// </summary>


        RemoteRunRequest,
        RemoteRunAllowed,
        RemoteRunDenied,

        StreamRequest,
        StreamResponse,

        DisconnectRequest = 0x110000,

        
    };
}