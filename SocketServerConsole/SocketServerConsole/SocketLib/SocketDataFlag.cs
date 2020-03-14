using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketLib
{
    public enum SocketDataFlag : int
    {
        None = 0,

        DirectoryRequest,
        DirectoryResponse,
        DirectoryException,

        DownloadRequest,
        DownloadAllowed,
        DownloadDenied,

        DownloadPackageRequest = 0x101,
        DownloadPackageResponse,

        UploadBiasRequest,
        UploadStreamRequest,
        UploadAllowed,
        UploadDenied,

        DeleteRequest,
        DeleteAllowed,
        DeleteDenied,

        RemoteRunRequest,
        RemoteRunAllowed,
        RemoteRunDenied,

        StreamRequest,
        StreamBiasRequest,
        StreamResponse,

        DisconnectRequest = 0x100,
    };
}