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
using FileManager.Events;
using FileManager.Models;

namespace FileManager.SocketLib
{
    /// <summary>
    /// Socket 层收发 bytes 方法均在此处
    /// </summary>
    public static class SocketIO
    {
        /// <summary>
        /// 循环操作socket接收数据写入buffer, 收不到数据抛出异常
        /// 字节流接收应调用此方法
        /// </summary>
        /// <param name="socket">socket</param>
        /// <param name="buffer">需写入的缓冲区</param>
        /// <param name="size">receive 字节数, 为 -1 则接收buffer长度字节</param>
        /// <param name="offset">buffer写入字节偏移</param>
        public static void ReceiveBuffer(Socket socket, byte[] buffer, int size = -1, int offset = 0)
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

        /// 单次不定长数据收发格式
        /// 
        /// |----------------------------------------------------------------------------------|
        /// |          Sender                                 |         Receiver               |
        /// | ProxyHeader | HB32Header | ContentBytes ->      |                                |
        /// |                                    ( 若单个packet发送不完 )                      |
        /// | {                                               |      <-  2 bytes (无内容)      |
        /// |               HB32Header | ContentBytes ->      |                    } * N       |
        /// |----------------------------------------------------------------------------------|



        // 此方法目前仅在 SocketProxy 里面被调用, 应该可以用 SendBytes() 代替 [21.12.20]
        /// <summary>
        /// 调用此方法可以在发送包头前添加 addition_bytes (发送代理包头)
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="header"></param>
        /// <param name="addition_bytes"></param>
        public static void SendHeader(Socket socket, HB32Header header, byte[] addition_bytes)
        {
            header.Default4 = 0;
            header.TotalByteLength = 0;
            header.RemainByteLength = 0;
            header.ValidByteLength = 0;
            byte[] bytes = header.GetBytes(addition_bytes);
            socket.Send(bytes);

            /*
            if (addition_bytes.Length == 0)
            {
                socket.Send(header.GetBytes());
            }
            else
            {
                socket.Send(BytesConverter.Concatenate(addition_bytes, header.GetBytes()));
            }
            */
        }




        /// <summary>
        /// 发送 Socket 数据包, 过长的 byte流 会被拆成多个包发送
        /// 包头的 PacketCount , ByteLength 等参数会视 bytes 长度被修改
        /// 拆成多包发送时, 只有第一个包头会带 ProxyHeader 供 SocketProxy 或 SocketServer 解析
        /// 后面的包均为 HB32Header + HB32Data
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="proxyHeaderBytes">第一个包头会带 ProxyHeader 供 SocketProxy 或 SocketServer 解析</param>
        /// <param name="header"></param>
        /// <param name="bytes"></param>
        /// <param name="packetLength">单个包大小</param>
        public static void SendBytes(Socket socket, byte[] proxyHeaderBytes, HB32Header header, byte[] bytes, int packetLength)
        {
            if (HB32Encoding.UseLegacyHeader)
            {
                header.Default4 = Math.Max((bytes.Length - 1) / (HB32Encoding.DataSize) + 1, 1);
                header.TotalByteLength = bytes.Length;
                int remain = bytes.Length;
                header.RemainByteLength = 0;
                for (int offset = 0; offset < bytes.Length || offset == 0; offset += packetLength)
                {
                    header.ValidByteLength = Math.Min(bytes.Length - offset, HB32Encoding.DataSize);
                    byte[] header_bytes;
                    if (offset == 0)
                    {
                        header_bytes = header.GetBytes(proxyHeaderBytes);
                    }
                    else
                    {
                        header_bytes = header.GetBytes();
                    }
                    byte[] _toSend;
                    if (HB32Encoding.TransferEmptyBytes)
                    {
                        _toSend = new byte[header_bytes.Length + HB32Encoding.DataSize];
                        Array.Copy(header_bytes, 0, _toSend, 0, header_bytes.Length);
                        Array.Copy(bytes, offset, _toSend, header_bytes.Length, header.ValidByteLength);
                    }
                    else
                    {
                        _toSend = new byte[header_bytes.Length + header.ValidByteLength];
                        Array.Copy(header_bytes, 0, _toSend, 0, header_bytes.Length);
                        Array.Copy(bytes, offset, _toSend, header_bytes.Length, header.ValidByteLength);
                    }
                    socket.Send(_toSend);
                    /// 若当前数据包之后还有数据包, 在等待对方 发送2个byte 后发送
                    if (offset + HB32Encoding.DataSize < bytes.Length)
                    {
                        ReceiveBuffer(socket, new byte[2]);
                        //ReceiveHeader(socket, out _);
                    }
                    header.RemainByteLength++;
                }
            }
            else
            {
                //header.Default4 = Math.Max((bytes.Length - 1) / (HB32Encoding.DataSize) + 1, 1);
                socket.Send(proxyHeaderBytes);
                if (bytes.Length == 0)
                {
                    header.TotalByteLength = 0;
                    header.ValidByteLength = 0;
                    header.RemainByteLength = 0;
                    socket.Send(header.GetBytes());
                }
                else
                {
                    header.TotalByteLength = bytes.Length;
                    for (int offset = 0; offset < bytes.Length; offset += packetLength)
                    {
                        int valid = Math.Min(bytes.Length - offset, HB32Encoding.DataSize);
                        int remain = bytes.Length - offset - valid;
                        header.ValidByteLength = valid;
                        header.RemainByteLength = remain;
                        socket.Send(header.GetBytes());
                        socket.Send(bytes.Skip(offset).Take(valid).ToArray());
                        /// 若当前数据包之后还有数据包, 在等待对方 发送2个byte 后发送
                        if (remain > 0)
                        {
                            ReceiveBuffer(socket, new byte[2]);
                        }
                    }
                }

            }

        }


        /// <summary>
        /// Receive socket 只接收包头
        /// 在 ReceiveBytes() 中调用
        /// 但不可以处理 ProxyHeader (处理 ProxyHeader 方法应位于 SocketEndPoint 类中)
        /// 此方法只能用于对于字节流的Header部份截取处理
        /// 若要完整接收只含 Header 的字节流, 应使用 ReceiveBytes() 方法
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="header"></param>
        private static void ReceiveHeader(Socket socket, out HB32Header header)
        {
            byte[] bytes_header = new byte[HB32Encoding.HeaderSize];
            ReceiveBuffer(socket, bytes_header);
            header = HB32Header.ReadFromBytes(bytes_header);
        }


        /// <summary>
        /// 接收 Socket 数据包, 在接收不定长byte流时使用, 过长byte流会分开接收并拼接成byte数组
        /// 收到的数据只有包头时, 返回空byte数组
        /// 只能处理去掉 ProxyHeader 后的字节流, 所以 socket 接收数据应先经过 SocketEndPoint 类处理 ProxyHeader
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="header"></param>
        /// <param name="bytes"></param>
        public static void ReceiveBytes(Socket socket, out HB32Header header, out byte[] bytes)
        {
            if (HB32Encoding.UseLegacyHeader)
            {
                /// 通过包头判断byte流长度, 确定byte数组大小 包数量 等基本信息
                ReceiveHeader(socket, out header);
                /// 此时 socket 只接收了HB32Header包头长度的字节
                /// 对于 SendHeader() 只发送包头的数据
                /// 函数会直接返回空byte数组
                bytes = new byte[header.TotalByteLength];
                int offset = 0;     /// bytes 数组写入起点偏移量
                for (int i = 0; i < header.Default4; ++i)
                {
                    if (i == header.Default4 - 1)
                    {
                        /// 读取缓冲区中有效数据
                        if (header.ValidByteLength > 0)
                        {
                            ReceiveBuffer(socket, bytes, header.ValidByteLength, offset);
                        }
                        /// 读取缓冲区中剩余的无效数据
                        if (HB32Encoding.TransferEmptyBytes)
                        {
                            ReceiveBuffer(socket, new byte[HB32Encoding.DataSize - header.ValidByteLength]);
                        }
                    }
                    else
                    {
                        /// 读取缓冲区数据
                        ReceiveBuffer(socket, bytes, header.ValidByteLength, offset);
                        offset += header.ValidByteLength;
                        // /// 发送 StreamRequset header
                        // SendHeader(socket, new HB32Header { Flag = SocketPacketFlag.StreamRequest }, new byte[2]);
                        /// 发送 2 个byte, 获取下一个数据包
                        socket.Send(new byte[2]);
                        /// 读取下一个包头
                        ReceiveHeader(socket, out header);
                    }
                }
            }
            else
            {
                /// 通过包头判断byte流长度, 确定byte数组大小 包数量 等基本信息
                /// 此时 socket 只接收了HB32Header包头长度的字节
                ReceiveHeader(socket, out header);

                /// 对于 SendBytes() 只发送包头的数据
                /// 函数会直接返回空byte数组
                int total = header.TotalByteLength;
                if (total == 0)
                {
                    bytes = new byte[0];
                }
                else
                {
                    bytes = new byte[header.TotalByteLength];
                    for (int offset = 0; ;)
                    {
                        int valid = header.ValidByteLength;
                        int remain = header.RemainByteLength;
                        ReceiveBuffer(socket, bytes, valid, offset);
                        offset += valid;
                        if (remain == 0)
                        {
                            break;
                        }
                        else
                        {
                            socket.Send(new byte[2]);
                            ReceiveHeader(socket, out header);
                        }
                    }
                }
            }
        }


        #endregion


    }
}
