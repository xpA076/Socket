using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using SocketServerConsole;

namespace SocketLib
{
    public class SocketIO
    {
        /// <summary>
        /// 循环操作socket接收数据写入buffer, 收不到数据抛出异常
        /// 字节流发送与接收应调用此方法
        /// </summary>
        /// <param name="socket">socket</param>
        /// <param name="buffer">缓冲区</param>
        /// <param name="size">receive 字节数, 为零则接收buffer长度字节</param>
        /// <param name="offset">buffer写入字节偏移</param>
        private void ReceiveBuffer(Socket socket, byte[] buffer, int size = -1, int offset = 0)
        {
            int _size = (size == -1) ? buffer.Length : size;
            int zeroReceiveCount = 0;
            int rec = 0;
            int _rec;

            _rec = socket.Receive(buffer, offset, _size, SocketFlags.None);
            if (_rec == 0) { zeroReceiveCount++; }
            rec += _rec;
            
            while (rec != _size)
            {
                _rec = socket.Receive(buffer, offset + rec, _size - rec, SocketFlags.None);
                if (_rec == 0) { zeroReceiveCount++; }
                rec += _rec;

                if (zeroReceiveCount > 2) { throw new Exception("Buffer receive error: cannot receive package"); }
            }
        }

        #region Socket 字节流 发送 与 接收

        /// <summary>
        /// Receive socket 只接收包头
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="header"></param>
        public void ReceiveHeader(Socket socket, out HB32Header header)
        {
            byte[] bytes_header = new byte[HB32Encoding.HeaderSize];
            ReceiveBuffer(socket, bytes_header);
            header = HB32Header.ReadFromBytes(bytes_header);
        }

        /// <summary>
        /// Send socket 只发送包头
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="header"></param>
        public void SendHeader(Socket socket, HB32Header header)
        {
            socket.Send(header.GetBytes(), HB32Encoding.HeaderSize, SocketFlags.None);
        }

        /// <summary>
        /// Receive Socket 数据包, 在确定接收数据包只有一个时使用, 输出 包头 和 byte数组格式内容
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="header">输出包头</param>
        /// <param name="bytes_data"></param>
        public void ReceivePackage(Socket socket, out HB32Header header, out byte[] bytes_data)
        {
            ReceiveHeader(socket, out header);

            byte[] _bytes_data = new byte[HB32Encoding.DataSize];
            ReceiveBuffer(socket,_bytes_data);
            bytes_data = _bytes_data;
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
                    ReceiveHeader(socket, out HB32Header response_header);
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
            // 通过包头判断byte流长度, 确定byte数组大小 包数量 等基本信息
            ReceiveHeader(socket, out header);
            // 此时 socket 只接收了HB32Header包头长度的字节
            // 当包头Flag为指定几种 SocketDataFlag 时 :
            // *** 这几种flag代表client只发了不带数据的包头过来 ***
            // 函数应直接返回空byte数组
            if (((int)header.Flag & 0x100) >0)
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
                    if (header.ValidByteLength > 0)
                    {
                        ReceiveBuffer(socket, bytes, header.ValidByteLength, offset);
                    }
                    // 读取缓冲区中剩余的无效数据
                    ReceiveBuffer(socket, new byte[HB32Encoding.DataSize - header.ValidByteLength]);
                }
                else
                {
                    // 读取缓冲区数据
                    ReceiveBuffer(socket, bytes, header.ValidByteLength, offset);
                    offset += header.ValidByteLength;
                    // 发送 StreamRequset header
                    SendHeader(socket, new HB32Header { Flag = SocketDataFlag.StreamRequest });
                    // 读取下一个包头
                    ReceiveHeader(socket, out header);
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
        public SokcetFileClass[] RequestDirectory(Socket socket, string path)
        {
            SendBytes(socket, new HB32Header { Flag = SocketDataFlag.DirectoryRequest }, path);
            ReceiveBytes(socket, out HB32Header header, out byte[] bytes);
            if (header.Flag != SocketDataFlag.DirectoryResponse)
            {
                throw new Exception(Encoding.UTF8.GetString(bytes));
            }
            SendHeader(socket, new HB32Header { Flag = SocketDataFlag.StreamRequest });
            ReceiveJson(socket, out header, out SokcetFileClass[] fileClasses);
            return fileClasses;
        }

        // 响应对方的 Directory 列表查询, 文件夹不存在或权限异常返回message字符串处理
        // 参数 bytes 为接收byte流的内容信息
        public void ResponseDirectory(Socket socket, byte[] bytes)
        {
            string path = Encoding.UTF8.GetString(bytes);
            SokcetFileClass[] fileClasses;
            try
            {
                fileClasses = GetDirectoryAndFiles(path);
                SendBytes(socket, new HB32Header { Flag = SocketDataFlag.DirectoryResponse }, new byte[0]);
            }
            catch (Exception ex)
            {
                SendBytes(socket, new HB32Header { Flag = SocketDataFlag.DirectoryException }, 
                    "Directory response exception from server: " + ex.Message);
                return;
            }
            ReceiveHeader(socket, out HB32Header _header);
            SendJson(socket, new HB32Header { Flag = SocketDataFlag.DirectoryResponse }, fileClasses);
        }

        // 获取本地指定路径下文件与文件夹列表
        // 异常： DirectoryNotFoundException, SecurityException
        private SokcetFileClass[] GetDirectoryAndFiles(string path)
        {
            List<SokcetFileClass> list = new List<SokcetFileClass>();
            if (string.IsNullOrEmpty(path))
            {
                /*
                List<string> disks = new List<string>()
                {
                    @"C:",
                    @"D:",
                    @"E:",
                    @"F:",
                    @"G:",
                    @"H:",
                    @"I:",
                    
                    //@"E:\Study\THz\comsol\reference\server\comsol"
                };
                */
                foreach (string _path in Config.AllowDirectoryList)
                {
                    if (Directory.Exists(_path))
                    {
                        list.Add(new SokcetFileClass { Name = _path, IsDirectory = true });
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
                        long len = GetDirectoryLength(directoryInfo.FullName);
                        list.Add(new SokcetFileClass
                        {
                            Name = directoryInfo.Name,
                            Length = len,
                            IsDirectory = true,
                        });
                    }
                    catch (Exception) {; }
                }
                foreach (FileInfo fileInfo in fileInfos)
                {
                    list.Add(new SokcetFileClass {
                        Name = fileInfo.Name,
                        Length = fileInfo.Length,
                        IsDirectory = false,
                    });
                }
                list.Sort(SokcetFileClass.Compare);
                return list.ToArray();
            }
        }

        private long GetDirectoryLength(string path)
        {
            long length = 0;
            DirectoryInfo info = new DirectoryInfo(path);
            foreach(FileInfo fi in info.GetFiles())
            {
                length += fi.Length;
            }
            foreach(DirectoryInfo di in info.GetDirectories())
            {
                length += GetDirectoryLength(di.FullName);
            }
            return length;
        }

        #endregion

        // TODO 文件byte传输部分还没有写
        #region 文件传输: 下载 上传 删除 (路径创建与删除)


        public void FileUploadResponse(Socket socket, HB32Header header, byte[] bytes)
        {
            FileStream remoteStream;
            try
            {
                string path = Encoding.UTF8.GetString(bytes);
                remoteStream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write);
            }
            catch (Exception ex)
            {
                SendBytes(socket, new HB32Header { Flag = SocketDataFlag.UploadDenied }, ex.Message);
                return;
            }
            remoteStream.Seek((long)header.I2 * (1 << 30) + (long)header.I3, SeekOrigin.Begin);
            while (true)
            {
                ReceivePackage(socket, out HB32Header recv_header, out byte[] recv_bytes);
                remoteStream.Write(recv_bytes, 0, recv_header.ValidByteLength);
                SendHeader(socket, new HB32Header { Flag = SocketDataFlag.UploadAllowed });
                if (recv_header.ValidByteLength == 0) { break; }
            }
            remoteStream.Close();
        }

        public void FileDelete(Socket socket, string remotePath)
        {
            SendBytes(socket, new HB32Header { Flag = SocketDataFlag.DeleteRequest }, remotePath);
            ReceivePackage(socket, out HB32Header header, out byte[] temp);
        }

        public void FileDeleteResponse(Socket socket, byte[] bytes)
        {
            string path = Encoding.UTF8.GetString(bytes);
            try
            {
                File.Delete(path);
            }
            catch (Exception ex)
            {
                SendBytes(socket, new HB32Header { Flag = SocketDataFlag.DeleteDenied }, ex.Message);
                return;
            }
            SendBytes(socket, new HB32Header { Flag = SocketDataFlag.DeleteAllowed }, new byte[0]);
        }
        #endregion

    }
}
