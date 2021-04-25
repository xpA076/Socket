using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SocketLib.SocketServer
{
    public partial class SocketServer : SocketServerBase
    {

        // 响应对方的 Directory 列表查询, 文件夹不存在或权限异常返回message字符串处理
        // 参数 bytes 为接收byte流的内容信息
        private void ResponseDirectory(Socket socket, byte[] bytes)
        {
            string path = Encoding.UTF8.GetString(bytes);
            SocketFileInfo[] fileClasses;
            try
            {
                fileClasses = GetDirectoryAndFiles(path);
                SendBytes(socket, SocketPacketFlag.DirectoryResponse, new byte[1]);
            }
            catch (Exception ex)
            {
                SendBytes(socket, SocketPacketFlag.DirectoryException,
                    "Directory response exception from server: " + ex.Message);
                return;
            }
            ReceiveHeader(socket, out _);
            SendBytes(socket, SocketPacketFlag.DirectoryResponse, SocketFileInfo.ListToBytes(fileClasses));
        }


        // 获取本地指定路径下文件与文件夹列表
        // 异常： DirectoryNotFoundException, SecurityException
        private SocketFileInfo[] GetDirectoryAndFiles(string path)
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
                return list.ToArray();
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
                return list.ToArray();
            }
        }


        private void ResponseDirectorySize(Socket client, byte[] bytes)
        {
            string path = Encoding.UTF8.GetString(bytes);
            long size = GetDirectorySize(path);
            SendBytes(client, SocketPacketFlag.DirectorySizeResponse, size.ToString());
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



        private void ResponseCreateDirectory(Socket client, byte[] bytes)
        {
            /// 验证 key
            int keyLength = Config.KeyLength;
            byte[] key = new byte[keyLength];
            Array.Copy(bytes, 0, key, 0, keyLength);
            try
            {
                if (!CheckKey(key)) { throw new Exception("Key error"); }
                string path = BytesParser.ParseString(bytes, ref keyLength);
                if (!Directory.Exists(path))
                {
                    DirectoryInfo dirInfo = new DirectoryInfo(path);
                    dirInfo.Create();
                }
                SendBytes(client, new HB32Header { Flag = SocketPacketFlag.CreateDirectoryAllowed }, new byte[1]);
            }
            catch (Exception ex)
            {
                SendBytes(client, new HB32Header { Flag = SocketPacketFlag.CreateDirectoryDenied }, ex.Message);
            }
        }


        private Dictionary<int, SocketServerFileStreamInfo> ServerFileSet = new Dictionary<int, SocketServerFileStreamInfo>();

    }
}
