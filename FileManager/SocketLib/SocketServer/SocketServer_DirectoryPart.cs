using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using FileManager.SocketLib.Enums;

namespace FileManager.SocketLib.SocketServer
{
    public partial class SocketServer : SocketServerBase
    {
        private Dictionary<int, SocketServerFileStreamInfo> ServerFileSet = new Dictionary<int, SocketServerFileStreamInfo>();

        private void ResponseDirectoryCheck(SocketResponder responder, byte[] bytes)
        {
            string path = Encoding.UTF8.GetString(bytes);
            if (Directory.Exists(path))
            {
                responder.SendHeader(SocketPacketFlag.DirectoryResponse);
            }
            else
            {
                responder.SendHeader(SocketPacketFlag.DirectoryException);
            }
        }



        /// <summary>
        /// 响应对方的 Directory 列表查询, 文件夹不存在或权限异常返回message字符串处理
        /// 参数 bytes 为接收byte流的内容信息
        /// client : SocketPacketFlag.DirectoryRequest + path bytes(UTF-8)
        /// server : SocketPacketFlag.DirectoryResponse + List<SocketFileInfo> -> bytes
        ///     or : SocketPacketFlag.DirectoryException + err_msg
        /// </summary>
        /// <param name="responder"></param>
        /// <param name="bytes"></param>
        private void ResponseDirectory(SocketResponder responder, byte[] bytes)
        {
            List<SocketFileInfo> fileClasses = new List<SocketFileInfo>();
            string err_msg = "";
            /// get SocketFileInfo[]
            try
            {
                if ((GetIdentity(responder) & SocketIdentity.ReadFile) == 0)
                {
                    throw new Exception("Socket not authenticated.");
                }
                string path = Encoding.UTF8.GetString(bytes);
                fileClasses = GetDirectoryAndFiles(path);
            }
            catch (Exception ex)
            {
                err_msg = "Directory response exception from server: " + ex.Message;
            }
            /// Send bytes
            if (string.IsNullOrEmpty(err_msg))
            {
                responder.SendBytes(SocketPacketFlag.DirectoryResponse, SocketFileInfo.ListToBytes(fileClasses));
            }
            else
            {
                responder.SendBytes(SocketPacketFlag.DirectoryException, err_msg);
            }
        }


        // 获取本地指定路径下文件与文件夹列表
        // 异常： DirectoryNotFoundException, SecurityException
        private List<SocketFileInfo> GetDirectoryAndFiles(string path)
        {
            List<SocketFileInfo> list = new List<SocketFileInfo>();
            if (string.IsNullOrEmpty(path))
            {
                foreach (string _path in Config.AllowDirectoryList)
                {
                    if (Directory.Exists(_path))
                    {
                        list.Add(new SocketFileInfo { Name = _path, IsDirectory = true });
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
                        list.Add(new SocketFileInfo
                        {
                            Name = directoryInfo.Name,
                            Length = 0,
                            IsDirectory = true,
                        });
                    }
                    catch (Exception) {; }
                }
                foreach (FileInfo fileInfo in fileInfos)
                {
                    list.Add(new SocketFileInfo
                    {
                        Name = fileInfo.Name,
                        Length = fileInfo.Length,
                        IsDirectory = false,
                    });
                }
                list.Sort(SocketFileInfo.Compare);
                return list;
            }
        }

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
                responder.SendBytes(SocketPacketFlag.CreateDirectoryAllowed, new byte[1]);
            }
            else
            {
                responder.SendBytes(SocketPacketFlag.CreateDirectoryDenied, err_msg);
            }
        }
        

    }
}
