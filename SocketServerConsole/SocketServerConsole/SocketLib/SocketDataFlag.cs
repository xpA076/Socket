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
        RequestFile,
        RequestJson,

        DirectoryRequest,
        DirectoryResponse,
        DirectoryException,

        DownloadBiasRequest,
        DownloadStreamRequest,
        DownloadAllowed,
        DownloadDenied,

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

        DisconnectRequest,
    };
}