using FileManager.Models.Serializable;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FileManager.Exceptions;
using FileManager.Exceptions.Server;
using FileManager.Models.SocketLib.SocketIO;
using FileManager.Models.SocketLib.Models;
using FileManager.Models.SocketLib.Enums;

namespace FileManager.Models.SocketLib.SocketServer.Main
{
    public partial class SocketServer : SocketServerBase
    {
        private Dictionary<int, SocketServerFileStreamInfo> ServerFileSet = new Dictionary<int, SocketServerFileStreamInfo>();


        /// <summary>
        /// 响应对方的 Directory 列表查询, 文件夹不存在或权限异常返回message字符串处理
        /// </summary>
        /// <param name="responder"></param>
        /// <param name="bytes"></param>
        private void ResponseDirectory(SocketResponder responder, DirectoryRequest request, SocketSession session)
        {
            try
            {
                if (request.Type == DirectoryRequest.RequestType.Query)
                {
                    /// 获取目录下的 SocketFileInfo 列表
                    if (!session.AllowQuery())
                    {
                        throw new SocketAuthenticationException();
                    }
                    List<SocketFileInfo> fileClasses = GetDirectoryAndFiles(request.ServerPath);
                    DirectoryResponse response = new DirectoryResponse(fileClasses);
                    this.Response(responder, response);
                }
                else if (request.Type == DirectoryRequest.RequestType.CreateDirectory)
                {
                    if (!session.AllowWriteFile())
                    {
                        throw new SocketAuthenticationException();
                    }
                    //todo
                    throw new NotImplementedException();
                }
                else
                {
                    throw new ServerInternalException();
                }
            }
            catch (SocketAuthenticationException)
            {
                string err_msg = "Authentication exception";
                DirectoryResponse response = new DirectoryResponse(err_msg);
                this.Response(responder, response);
            }
            catch (ServerInternalException ex)
            {
                string err_msg = "Directory response exception from server: " + ex.Message;
                DirectoryResponse response = new DirectoryResponse(err_msg);
                this.Response(responder, response);
            }
        }


        // 获取本地指定路径下文件与文件夹列表
        // 异常： DirectoryNotFoundException, SecurityException
        private List<SocketFileInfo> GetDirectoryAndFiles(string path)
        {
            try
            {
                List<SocketFileInfo> list = new List<SocketFileInfo>();
                if (string.IsNullOrEmpty(path))
                {
                    foreach (string _path in Config.AllowDirectoryList)
                    {
                        if (Directory.Exists(_path))
                        {
                            list.Add(new SocketFileInfo()
                            {
                                Name = _path, 
                                IsDirectory = true,
                                Length = 0,
                                CreationTimeUtc = new DateTime(0),
                                LastWriteTimeUtc = new DateTime(0)
                            });
                        }
                    }
                    return list;
                }
                else
                {
                    DirectoryInfo directory = new DirectoryInfo(path);
                    FileInfo[] fileInfos = directory.GetFiles();
                    DirectoryInfo[] directoryInfos = directory.GetDirectories();
                    foreach (DirectoryInfo directoryInfo in directoryInfos)
                    {
                        try
                        {
                            list.Add(new SocketFileInfo()
                            {
                                Name = directoryInfo.Name,
                                IsDirectory = true,
                                Length = 0,
                                CreationTimeUtc = new DateTime(0),
                                LastWriteTimeUtc = new DateTime(0)
                            });
                        }
                        catch (Exception) {; }
                    }
                    foreach (FileInfo fileInfo in fileInfos)
                    {
                        list.Add(new SocketFileInfo()
                        {
                            Name = fileInfo.Name,
                            IsDirectory = false,
                            Length = fileInfo.Length,
                            CreationTimeUtc = fileInfo.CreationTimeUtc,
                            LastWriteTimeUtc = fileInfo.LastWriteTimeUtc
                        });
                    }
                    list.Sort(SocketFileInfo.Compare);
                    return list;
                }
            }
            catch (Exception ex)
            {
                throw new ServerInternalException(ex.Message);
            }

        }

        /// <summary>
        
        /// </summary>
        /// <param name="server_dir"></param>
        private void CreateDirectory(string server_dir)
        {

        }


        /*
        /// <summary>
        /// 响应client请求文件大小
        /// client : SocketPacketFlag.DirectorySizeRequest + path bytes(UTF-8)
        /// server : SocketPacketFlag.DirectorySizeResponse + size.toString() -> UTF-8
        ///     or : SocketPacketFlag.DirectoryException + err_msg
        /// </summary>
        /// <param name="responder"></param>
        /// <param name="bytes"></param>
        private void ResponseDirectorySize(SocketResponder responder, byte[] bytes)
        {
            /// Get size
            long size = 0;
            string err_msg = "";
            try
            {
                if ((GetIdentity(responder) & SocketIdentity.ReadFile) == 0)
                {
                    throw new Exception("Socket not authenticated.");
                }
                string path = Encoding.UTF8.GetString(bytes);
                size = GetDirectorySize(path);
            }
            catch (Exception ex)
            {
                err_msg = ex.Message;
            }
            /// Send bytes
            if (string.IsNullOrEmpty(err_msg))
            {
                responder.SendBytes(SocketPacketFlag.DirectorySizeResponse, size.ToString());
            }
            else
            {
                responder.SendBytes(SocketPacketFlag.DirectoryException, err_msg);
            }
        }


        private long GetDirectorySize(string path)
        {
            long size = 0;
            DirectoryInfo dir = new DirectoryInfo(path);
            FileInfo[] fileInfos = dir.GetFiles();
            foreach (FileInfo fileInfo in fileInfos)
            {
                if (Config.IsPathAllowed(fileInfo.FullName))
                {
                    size += fileInfo.Length;
                }
            }
            DirectoryInfo[] directoryInfos = dir.GetDirectories();
            foreach (DirectoryInfo directoryInfo in directoryInfos)
            {
                if (Config.IsPathAllowed(directoryInfo.FullName))
                {
                    size += GetDirectorySize(directoryInfo.FullName);
                }
            }
            return size;
        }
        */

        // todo 这个争取不要 22.04.14
        /// <summary>
        /// 响应在server端创建目录请求
        /// client : SocketPacketFlag.CreateDirectoryRequest + (UTF-8)server目录名称
        /// server : SocketPacketFlag.CreateDirectoryAllowed + new byte[1]
        ///     or : SocketPacketFlag.CreateDirectoryDenied + err_msg
        /// </summary>
        /// <param name="responder"></param>
        /// <param name="bytes"></param>
        private void ResponseCreateDirectory(SocketResponder responder, byte[] bytes)
        {
            /*
            string err_msg = "";
            try
            {
                string path = Encoding.UTF8.GetString(bytes);
                if (!Directory.Exists(path))
                {
                    DirectoryInfo dirInfo = new DirectoryInfo(path);
                    dirInfo.Create();
                }
            }
            catch (Exception ex)
            {
                err_msg = ex.Message;
            }
            if (string.IsNullOrEmpty(err_msg))
            {
                responder.SendBytes(PacketType.CreateDirectoryAllowed, new byte[1]);
            }
            else
            {
                responder.SendBytes(PacketType.CreateDirectoryDenied, err_msg);
            }
            */
        }
        

    }
}
