using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

namespace SocketFileManager.SocketLib
{
    public abstract class SocketIO
    {
        #region Socket 字节流 发送 与 接收

        // Receive Socket 数据包, 在确定接收数据包只有一个时使用, 输出 包头 和 byte数组格式内容
        public void ReceivePackage(Socket socket, out HB32Header header, out byte[] bytes_data)
        {
            byte[] bytes_header = new byte[HB32Encoding.HeaderSize];
            byte[] _bytes_data = new byte[HB32Encoding.DataSize];
            int rec = socket.Receive(bytes_header, 0, HB32Encoding.HeaderSize, SocketFlags.None);
            while (rec != HB32Encoding.HeaderSize)
            {
                rec += socket.Receive(bytes_header, rec, bytes_header.Length - rec, SocketFlags.None);
            }
            header = HB32Header.ReadFromBytes(bytes_header);
            rec = socket.Receive(_bytes_data, 0, HB32Encoding.DataSize, SocketFlags.None);
            while (rec != HB32Encoding.DataSize)
            {
                rec += socket.Receive(_bytes_data, rec, _bytes_data.Length - rec, SocketFlags.None);
            }
            bytes_data = _bytes_data;
        }

        public void ReceivePackage(Socket socket, SocketDataFlag matchFlag, out HB32Header header, out byte[] bytes_data)
        {
            ReceivePackage(socket, out header, out bytes_data);
            if (header.Flag != matchFlag)
            {
                throw (new Exception("Invalid socket stream header: cannot match " + matchFlag.ToString()));
            }
        }
        // 只接收包头
        public void ReceiveHeader(Socket socket, out HB32Header header)
        {
            byte[] bytes_header = new byte[HB32Encoding.HeaderSize];
            int rec = socket.Receive(bytes_header, 0, HB32Encoding.HeaderSize, SocketFlags.None);
            while (rec != HB32Encoding.HeaderSize)
            {
                rec += socket.Receive(bytes_header, rec, bytes_header.Length - rec, SocketFlags.None);
            }
            header = HB32Header.ReadFromBytes(bytes_header);
        }

        public void ReceiveHeader(Socket socket, SocketDataFlag matchFlag, out HB32Header header)
        {
            ReceiveHeader(socket, out header);
            if (header.Flag != matchFlag)
            {
                throw (new Exception("Invalid socket header: cannot match " + matchFlag.ToString()));
            }
        }

        public void SendHeader(Socket socket, HB32Header header)
        {
            socket.Send(header.GetBytes(), HB32Encoding.HeaderSize, SocketFlags.None);
        }

        // 发送 Socket 数据包, 过长的 byte流 会被拆开发送, 包头的count,length等参数会视byte长度被修改
        public void SendBytes(Socket socket, HB32Header header, byte[] bytes)
        {
            header.PackageCount = ((bytes.Length > 0 ? bytes.Length : 1) - 1) / (HB32Encoding.DataSize) + 1;
            header.TotalByteLength = bytes.Length;
            header.PackageIndex = 0;
            for (int offset = 0; offset < bytes.Length || offset == 0; offset += HB32Encoding.DataSize)
            {
                if (bytes.Length - offset < HB32Encoding.DataSize)
                {
                    header.ValidByteLength = bytes.Length - offset;
                    socket.Send(header.GetBytes(), HB32Encoding.HeaderSize, SocketFlags.None);
                    socket.Send(bytes, offset, bytes.Length - offset, SocketFlags.None);
                    socket.Send(new byte[HB32Encoding.DataSize - (bytes.Length - offset)], HB32Encoding.DataSize - (bytes.Length - offset), SocketFlags.None);
                }
                else
                {
                    header.ValidByteLength = HB32Encoding.DataSize;
                    socket.Send(header.GetBytes(), HB32Encoding.HeaderSize, SocketFlags.None);
                    socket.Send(bytes, offset, HB32Encoding.DataSize, SocketFlags.None);
                }
                // 若当前数据包之后还有数据包, 在等待对方 发送StreamRequest包头 后发送
                if (offset + HB32Encoding.DataSize < bytes.Length)
                {
                    ReceiveHeader(socket, SocketDataFlag.StreamRequest, out HB32Header response_header);
                }
                header.PackageIndex++;
            }
        }
        // 发送 Socket 数据包, 字符串以 UTF-8 编码后发送
        public void SendBytes(Socket socket, HB32Header header, string str)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(str);
            SendBytes(socket, header, bytes);
        }

        // 接收 Socket 数据包, 在接收不定长byte流时使用, 过长byte流会分开接收并拼接成byte数组
        // 当包头Flag为指定几种 SocketDataFlag 时，直接返回空byte数组
        public void ReceiveBytes(Socket socket, out HB32Header header, out byte[] bytes)
        {
            byte[] bytes_header = new byte[HB32Encoding.HeaderSize];
            int rec = socket.Receive(bytes_header, 0, HB32Encoding.HeaderSize, SocketFlags.None);
            while (rec != HB32Encoding.HeaderSize)
            {
                rec += socket.Receive(bytes_header, rec, bytes_header.Length - rec, SocketFlags.None);
            }
            // 通过包头判断byte流长度, 确定byte数组大小 包数量 等基本信息
            header = HB32Header.ReadFromBytes(bytes_header);
            // 此时 socket 只接收了HB32Header包头长度的字节
            // 当包头Flag为指定几种 SocketDataFlag 时 :
            // *** 这几种flag代表client只发了不带数据的包头过来 ***
            // 函数应直接返回空byte数组
            if (header.Flag == SocketDataFlag.DownloadStreamRequest)
            {
                bytes = new byte[0];
                return;
            }
            bytes = new byte[header.TotalByteLength];
            int offset = 0;     // bytes 数组写入起点偏移量
            for (int i = 0; i < header.PackageCount; ++i)
            {
                if (i == header.PackageCount - 1)
                {
                    // 读取缓冲区中有效数据
                    rec = socket.Receive(bytes, offset, header.ValidByteLength, SocketFlags.None);
                    while (rec != header.ValidByteLength)
                    {
                        rec += socket.Receive(bytes, offset + rec, header.ValidByteLength - rec, SocketFlags.None);
                    }
                    // 读取缓冲区中剩余的无效数据
                    while (rec != HB32Encoding.DataSize)
                    {
                        rec += socket.Receive(new byte[HB32Encoding.DataSize - rec], 0, HB32Encoding.DataSize - rec, SocketFlags.None);
                    }
                }
                else
                {
                    // 读取缓冲区数据
                    rec = socket.Receive(bytes, offset, header.ValidByteLength, SocketFlags.None);
                    while (rec != HB32Encoding.DataSize)
                    {
                        rec += socket.Receive(bytes, offset + rec, HB32Encoding.DataSize - rec, SocketFlags.None);
                    }
                    offset += rec;
                    // 发送 StreamRequset header
                    SendHeader(socket, new HB32Header { Flag = SocketDataFlag.StreamRequest });
                    // 读取下一个包头
                    rec = socket.Receive(bytes_header, 0, bytes_header.Length, SocketFlags.None);
                    while (rec != HB32Encoding.HeaderSize)
                    {
                        rec += socket.Receive(bytes_header, rec, bytes_header.Length - rec, SocketFlags.None);
                    }
                    header = HB32Header.ReadFromBytes(bytes_header);
                }
            }
        }

        // 对象序列化, 以Json格式发送
        public void SendJson(Socket socket, HB32Header header, object obj)
        {
            System.Web.Script.Serialization.JavaScriptSerializer jss = new System.Web.Script.Serialization.JavaScriptSerializer();
            string json = jss.Serialize(obj);
            SendBytes(socket, header, json);
        }

        // 接收字符串并以Json反序列化
        public void ReceiveJson<T>(Socket socket, out HB32Header header, out T obj)
        {
            System.Web.Script.Serialization.JavaScriptSerializer jss = new System.Web.Script.Serialization.JavaScriptSerializer();
            ReceiveBytes(socket, out header, out byte[] bytes);
            string json = Encoding.UTF8.GetString(bytes);
            obj = jss.Deserialize<T>(json);
        }

        #endregion

        #region 文件列表请求 发送与接收

        // 请求对方本地 path 路径下的 Directory 列表 ( path为空则请求盘符信息 )
        // 列表请求异常则返回null, message输出异常信息
        public FileClass[] RequestDirectory(Socket socket, string path, out string message)
        {
            SendBytes(socket, new HB32Header { Flag = SocketDataFlag.DirectoryRequest }, path);
            ReceiveBytes(socket, out HB32Header header, out byte[] bytes);
            if (header.Flag != SocketDataFlag.DirectoryResponse)
            {
                message = Encoding.UTF8.GetString(bytes);
                return null;
            }
            SendBytes(socket, new HB32Header { Flag = SocketDataFlag.StreamRequest }, new byte[0]);
            ReceiveJson(socket, out header, out FileClass[] fileClasses);
            message = "";
            return fileClasses;
        }

        // 响应对方的 Directory 列表查询, 文件夹不存在或权限异常返回message字符串处理
        // 参数 bytes 为接收byte流的内容信息
        public void ResponseDirectory(Socket socket, byte[] bytes)
        {
            string path = Encoding.UTF8.GetString(bytes);
            FileClass[] fileClasses;
            try
            {
                fileClasses = GetDirectoryAndFiles(path);
                SendBytes(socket, new HB32Header { Flag = SocketDataFlag.DirectoryResponse }, new byte[0]);
            }
            catch (Exception ex)
            {
                SendBytes(socket, new HB32Header { Flag = SocketDataFlag.DirectoryException }, ex.Message);
                return;
            }
            ReceiveBytes(socket, out HB32Header _header, out byte[] _bytes);
            SendJson(socket, new HB32Header { Flag = SocketDataFlag.DirectoryResponse }, fileClasses);
        }

        // 获取本地指定路径下文件与文件夹列表
        // 异常： DirectoryNotFoundException, SecurityException
        private static FileClass[] GetDirectoryAndFiles(string path)
        {
            List<FileClass> list = new List<FileClass>();
            if (string.IsNullOrEmpty(path))
            {
                List<string> disks = new List<string>()
                {
                    @"C:",
                    @"D:",
                    @"E:",
                    @"F:",
                    @"G:",
                    @"H:",
                    @"I:",
                };
                foreach (string disk in disks)
                {
                    if (Directory.Exists(disk))
                    {
                        list.Add(new FileClass { Name = disk, IsDirectory = true });
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
                    list.Add(new FileClass { Name = directoryInfo.Name, IsDirectory = true });
                }

                foreach (FileInfo fileInfo in fileInfos)
                {
                    list.Add(new FileClass { Name = fileInfo.Name, Length = fileInfo.Length, IsDirectory = false });
                }

                list.Sort(FileClass.Compare);
                return list.ToArray();
            }
        }


        #endregion

        // TODO 文件byte传输部分还没有写

        // TODO 看看怎么改
        public abstract void Close();
        public void Disconnect(Socket socket)
        {
            SendBytes(socket, new HB32Header { Flag = SocketDataFlag.DisconnectRequest }, new byte[0]);
            Close();
        }
    }
}
