using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

using FileManager.SocketLib.Enums;

namespace FileManager.SocketLib
{
    public class SocketIO
    {
        public GetHeaderBytesHandler GetHeaderBytesFunc = HB32Header.GetBytes;


        /// <summary>
        /// 循环操作socket接收数据写入buffer, 收不到数据抛出异常
        /// 字节流发送与接收应调用此方法
        /// </summary>
        /// <param name="socket">socket</param>
        /// <param name="buffer">缓冲区</param>
        /// <param name="size">receive 字节数, 为 -1 则接收buffer长度字节</param>
        /// <param name="offset">buffer写入字节偏移</param>
        protected void ReceiveBuffer(Socket socket, byte[] buffer, int size = -1, int offset = 0)
        {
            if (buffer.Length == 0) { return; }
            int _size = (size == -1) ? buffer.Length : size;
            int zeroReceiveCount = 0;
            int rec = 0;    // 函数内累计接收字节数
            int _rec;       // 单个 Socket.Receive 调用接收字节数

            _rec = socket.Receive(buffer, offset, _size, SocketFlags.None);
            if (_rec == 0) { zeroReceiveCount++; }
            rec += _rec;

            while (rec != _size)
            {
                _rec = socket.Receive(buffer, offset + rec, _size - rec, SocketFlags.None);
                if (_rec == 0) { zeroReceiveCount++; }
                rec += _rec;
                if (zeroReceiveCount > 3) { throw new Exception("Buffer receive error: cannot receive package"); }
            }
        }

        #region Socket 字节流 发送 与 接收



        /// <summary>
        /// Send socket 只发送包头
        /// 只有 SendHeader() 会只发送包头
        /// SendBytes() 至少会发一个空包
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="header"></param>
        protected void SendHeader(Socket socket, HB32Header header)
        {
            header.PacketCount = 0;
            header.TotalByteLength = 0;
            header.PacketIndex = 0;
            header.ValidByteLength = 0;
            byte[] header_bytes = GetHeaderBytesFunc(header);
            socket.Send(header_bytes, header_bytes.Length, SocketFlags.None);
        }


        /// <summary>
        /// Send socket 只发送包头
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="flag"></param>
        /// <param name="i1"></param>
        /// <param name="i2"></param>
        /// <param name="i3"></param>
        protected void SendHeader(Socket socket, SocketPacketFlag flag, int i1 = 0, int i2 = 0, int i3 = 0)
        {
            SendHeader(socket, new HB32Header
            {
                Flag = flag,
                I1 = i1,
                I2 = i2,
                I3 = i3
            });
        }


        /// <summary>
        /// 发送 Socket 数据包, 过长的 byte流 会被拆成多个包发送
        /// 包头的 PacketCount , ByteLength 等参数会视 bytes 长度被修改
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="header"></param>
        /// <param name="bytes"></param>
        protected void SendBytes(Socket socket, HB32Header header, byte[] bytes)
        {
            header.PacketCount = ((bytes.Length > 0 ? bytes.Length : 1) - 1) / (HB32Encoding.DataSize) + 1;
            header.TotalByteLength = bytes.Length;
            header.PacketIndex = 0;
            for (int offset = 0; offset < bytes.Length || offset == 0; offset += HB32Encoding.DataSize)
            {
                if (bytes.Length - offset < HB32Encoding.DataSize)
                {
                    header.ValidByteLength = bytes.Length - offset;
                    byte[] header_bytes = GetHeaderBytesFunc(header);
                    byte[] _toSend = new byte[header_bytes.Length + HB32Encoding.DataSize];
                    Array.Copy(header_bytes, 0, _toSend, 0, header_bytes.Length);
                    Array.Copy(bytes, offset, _toSend, header_bytes.Length, bytes.Length - offset);
                    socket.Send(_toSend);
                }
                else
                {
                    header.ValidByteLength = HB32Encoding.DataSize;
                    byte[] header_bytes = GetHeaderBytesFunc(header);
                    byte[] _toSend = new byte[header_bytes.Length + HB32Encoding.DataSize];
                    Array.Copy(header_bytes, 0, _toSend, 0, header_bytes.Length);
                    Array.Copy(bytes, offset, _toSend, header_bytes.Length, HB32Encoding.DataSize);
                    socket.Send(_toSend);
                }
                /// 若当前数据包之后还有数据包, 在等待对方 发送StreamRequest包头 后发送
                if (offset + HB32Encoding.DataSize < bytes.Length)
                {
                    ReceiveHeader(socket, out _);
                }
                header.PacketIndex++;
            }
        }



        /// <summary>
        /// 发送 Socket 数据包, 字符串以 UTF-8 编码后发送
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="header"></param>
        /// <param name="str"></param>
        protected void SendBytes(Socket socket, HB32Header header, string str)
        {
            SendBytes(socket, header, Encoding.UTF8.GetBytes(str));
        }

        /// <summary>
        /// 发送 Socket 数据包
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="flag"></param>
        /// <param name="bytes"></param>
        /// <param name="i1"></param>
        /// <param name="i2"></param>
        /// <param name="i3"></param>
        protected void SendBytes(Socket socket, SocketPacketFlag flag, byte[] bytes, int i1 = 0, int i2 = 0, int i3 = 0)
        {
            SendBytes(socket, new HB32Header
            {
                Flag = flag,
                I1 = i1,
                I2 = i2,
                I3 = i3
            }, bytes);
        }

        /// <summary>
        /// 发送 Socket 数据包, 字符串以 UTF-8 编码后发送
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="flag"></param>
        /// <param name="str"></param>
        /// <param name="i1"></param>
        /// <param name="i2"></param>
        /// <param name="i3"></param>
        protected void SendBytes(Socket socket, SocketPacketFlag flag, string str, int i1 = 0, int i2 = 0, int i3 = 0)
        {
            SendBytes(socket, flag, Encoding.UTF8.GetBytes(str), i1, i2, i3);
        }


        /// <summary>
        /// Receive socket 只接收包头
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="header"></param>
        protected void ReceiveHeader(Socket socket, out HB32Header header)
        {
            byte[] bytes_header = new byte[HB32Encoding.HeaderSize];
            ReceiveBuffer(socket, bytes_header);
            header = HB32Header.ReadFromBytes(bytes_header);
        }


        /// <summary>
        /// 尽量不要用, 可以用 ReceiveBytes() 代替
        /// Receive Socket 数据包, 在确定接收数据包只有一个时使用, 输出 包头 和 byte数组格式内容
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="header">输出包头</param>
        /// <param name="bytes_data"></param>
        protected void ReceivePacket(Socket socket, out HB32Header header, out byte[] bytes_data)
        {
            ReceiveHeader(socket, out header);
            bytes_data = new byte[HB32Encoding.DataSize];
            ReceiveBuffer(socket, bytes_data);
        }



        /// 接收 Socket 数据包, 在接收不定长byte流时使用, 过长byte流会分开接收并拼接成byte数组
        /// 收到的数据只有包头时, 返回空byte数组
        protected void ReceiveBytes(Socket socket, out HB32Header header, out byte[] bytes)
        {
            /// 通过包头判断byte流长度, 确定byte数组大小 包数量 等基本信息
            ReceiveHeader(socket, out header);
            /// 此时 socket 只接收了HB32Header包头长度的字节
            /// 对于 SendHeader() 只发送包头的数据
            /// 函数会直接返回空byte数组
            /*
            if (header.Flag == SocketPacketFlag.DownloadPacketRequest ||
                header.Flag == SocketPacketFlag.UploadPacketResponse)
            {
                bytes = new byte[0];
                return;
            }
            */
            bytes = new byte[header.TotalByteLength];
            int offset = 0;     // bytes 数组写入起点偏移量
            for (int i = 0; i < header.PacketCount; ++i)
            {
                if (i == header.PacketCount - 1)
                {
                    /// 读取缓冲区中有效数据
                    if (header.ValidByteLength > 0)
                    {
                        ReceiveBuffer(socket, bytes, header.ValidByteLength, offset);
                    }
                    /// 读取缓冲区中剩余的无效数据
                    ReceiveBuffer(socket, new byte[HB32Encoding.DataSize - header.ValidByteLength]);
                }
                else
                {
                    /// 读取缓冲区数据
                    ReceiveBuffer(socket, bytes, header.ValidByteLength, offset);
                    offset += header.ValidByteLength;
                    /// 发送 StreamRequset header
                    SendHeader(socket, SocketPacketFlag.StreamRequest);
                    /// 读取下一个包头
                    ReceiveHeader(socket, out header);
                }
            }
        }


        #endregion
    }
}
