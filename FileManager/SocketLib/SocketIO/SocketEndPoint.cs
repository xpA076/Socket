using FileManager.Exceptions;
using FileManager.SocketLib.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.IO;
using FileManager.Models.Serializable.Crypto;
using FileManager.SocketLib.HbProtocol;

namespace FileManager.SocketLib
{
    /// <summary>
    /// Socket 连接中的 client / server 端, 有简单封装的 Connect / SendBytes / ReceiveBytes 方法
    /// </summary>

    public class SocketEndPoint
    {
        protected Socket socket = null;

        protected bool EncryptFlag
        {
            get
            {
                return AesKeys != null;
            }
        }
        protected byte[] AesKeys = null;

        /// <summary>
        /// 当前 SocketEndPoint 是否为向代理端主动通信的对象
        /// 这种情况下数据通信需要额外添加代理包头
        /// </summary>
        public bool IsRequireProxyHeader { get; protected set; } = false;


        public void SetTimeout(int send_timeout, int receive_timeout)
        {
            socket.SendTimeout = send_timeout;
            socket.ReceiveTimeout = receive_timeout;
        }


        public void SetSymmetricKeys(byte[] keys)
        {
            this.AesKeys = new byte[keys.Length];
            Array.Copy(keys, this.AesKeys, keys.Length);
        }


        #region Send/Receive bytes

        private void ReceiveBuffer(byte[] buffer, int size = -1, int offset = 0)
        {
            if (buffer.Length == 0) { return; }
            int _size = (size < 0) ? buffer.Length : size;
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
                if (zeroReceiveCount > 3) { throw new SocketConnectionException("Buffer receive error: cannot receive package"); }

            }
        }

        private void SendBytesIO(UInt32 u32h, byte[] bytes, int truncateLength)
        {
            /// Assert : bytes.Length > 0
            HB16Header header = new HB16Header();
            header.U32Header = u32h;
            header.TotalByteLength = bytes.Length;
            for (int offset = 0; offset < bytes.Length; offset += truncateLength)
            {
                int valid = Math.Min(bytes.Length - offset, truncateLength);
                int remain = bytes.Length - offset - valid;
                header.ValidByteLength = valid;
                header.RemainByteLength = remain;
                byte[] toSend = new byte[HB16Header.Length + valid];
                Array.Copy(header.GetBytes(), toSend, HB16Header.Length);
                Array.Copy(bytes, offset, toSend, HB16Header.Length, valid);
                this.socket.Send(toSend);
                if (remain > 0)
                {
                    this.ReceiveBuffer(new byte[2]);
                }
            }
        }

        private void ReceiveBytesIO(out byte[] bytes, out UInt32 u32h)
        {
            /// Receive header
            byte[] headerBytes = new byte[HB16Header.Length];
            this.ReceiveBuffer(headerBytes);
            HB16Header header = new HB16Header(headerBytes);
            u32h = header.U32Header;

            /// Receive bytes
            bytes = new byte[header.TotalByteLength];
            for (int offset = 0; ;)
            {
                int valid = header.ValidByteLength;
                int remain = header.RemainByteLength;
                this.ReceiveBuffer(bytes, valid, offset);
                offset += valid;
                if (remain == 0)
                {
                    break;
                }
                else
                {
                    this.socket.Send(new byte[2]);
                    this.ReceiveBuffer(headerBytes);
                    header = new HB16Header(headerBytes);
                }
            }
        }

        /// <summary>
        /// Send by HB16 protocol
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="truncateLength"></param>
        /// <param name="encryptText"></param>
        public void SendBytes(byte[] bytes, int truncateLength = 4096, bool encryptText = true)
        {
            if (encryptText)
            {
                if (this.AesKeys == null) { throw new Exception("AES encrypt without key setup"); }
                AesEncryptedBytes enc = new AesEncryptedBytes();
                using (Aes aes = Aes.Create())
                {
                    aes.Key = this.AesKeys;
                    enc.IV = aes.IV;
                    using (MemoryStream cipherText = new MemoryStream())
                    using (CryptoStream cs = new CryptoStream(cipherText, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(bytes, 0, bytes.Length);
                        cs.Close();
                        enc.EncryptedBytes = cipherText.ToArray();
                    }
                }
                SendBytesIO((UInt32)PacketType.TextEncrypted, enc.ToBytes(), truncateLength);
            }
            else
            {
                SendBytesIO((UInt32)PacketType.TextPlain, bytes, truncateLength);
            }
            
        }

        /// <summary>
        /// Receive by HB16 protocol
        /// </summary>
        /// <returns></returns>
        public byte[] ReceiveBytes()
        {
            ReceiveBytesIO(out byte[] bytes, out UInt32 u32h);
            if (u32h == (UInt32)PacketType.TextPlain) { return bytes; }
            if (u32h != (UInt32)PacketType.TextEncrypted) { throw new Exception("Invalid header in receive bytes"); }
            if (this.AesKeys == null) { throw new Exception("Need decrpypt before AES key setup"); }
            /// Decrpyt bytes
            AesEncryptedBytes enc = AesEncryptedBytes.FromBytes(bytes);
            using (Aes aes = Aes.Create())
            {
                aes.Key = this.AesKeys;
                aes.IV = enc.IV;
                using (MemoryStream plainText = new MemoryStream())
                using (CryptoStream cs = new CryptoStream(plainText, aes.CreateDecryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(enc.EncryptedBytes, 0, enc.EncryptedBytes.Length);
                    cs.Close();
                    return plainText.ToArray();
                }
            }
        }

        #endregion

        #region Send / Receive


        public void SendHeader(HB32Header header)
        {
            if (IsRequireProxyHeader)
            {
                SocketIO.SendHeader(socket, header, new byte[2] { SocketProxy.ProxyHeaderByte, (byte)ProxyHeader.SendHeader });
            }
            else
            {
                SocketIO.SendHeader(socket, header, new byte[2] { 0, 0 });
            }
        }


        public void SendHeader(PacketType flag, int i1 = 0, int i2 = 0, int i3 = 0)
        {
            SendHeader(new HB32Header
            {
                Flag = flag,
                I1 = i1,
                I2 = i2,
                I3 = i3
            });
        }

        public void SendBytes(HB32Header header, byte[] bytes, int packetLength)
        {
            if (IsRequireProxyHeader)
            {
                SocketIO.SendBytes(socket, new byte[2] { SocketProxy.ProxyHeaderByte, (byte)ProxyHeader.SendBytes },
                    header, bytes, packetLength);
            }
            else
            {
                SocketIO.SendBytes(socket, new byte[2] { 0, 0 },
                    header, bytes, packetLength);
            }

        }

        public void SendBytes(HB32Header header, byte[] bytes)
        {
            SendBytes(header, bytes, HB32Encoding.DataSize);
        }

        public void SendBytes(HB32Header header, string str)
        {
            SendBytes(header, Encoding.UTF8.GetBytes(str));
        }




        public void SendBytes(PacketType flag, byte[] bytes, int i1 = 0, int i2 = 0, int i3 = 0)
        {
            SendBytes(new HB32Header
            {
                Flag = flag,
                I1 = i1,
                I2 = i2,
                I3 = i3
            }, bytes);
        }

        public void SendBytes(PacketType flag, string str, int i1 = 0, int i2 = 0, int i3 = 0)
        {
            SendBytes(flag, Encoding.UTF8.GetBytes(str), i1, i2, i3);
        }


        public ProxyHeader ReceiveProxyHeader()
        {
            byte[] proxy_bytes = new byte[2];
            SocketIO.ReceiveBuffer(socket, proxy_bytes);
            if (proxy_bytes[0] == 0x01)
            {
                return (ProxyHeader)proxy_bytes[1];
            }
            return ProxyHeader.None;
        }


        public void ReceiveBytes(out HB32Header header, out byte[] bytes)
        {
            if (IsRequireProxyHeader)
            {
                socket.Send(new byte[2] { 0xA3, (byte)ProxyHeader.ReceiveBytes });
            }
            /// Receive 的数据仍有一个空的ProxyHeader, 应处理后再接收数据
            /// 2023.09.14 重写 SocketIO.SendBytes()/ReceiveBytes() 不需要接收 Header前的 2bytes
            //ReceiveProxyHeader();
            SocketIO.ReceiveBytes(socket, out header, out bytes);
        }

        public byte[] ReceiveBuffer(int length)
        {
            byte[] bs = new byte[length];
            SocketIO.ReceiveBuffer(socket, bs);
            //throw new Exception("cannot use this method");
            return bs;
        }


        public void ReceiveBytesWithHeaderFlag(PacketType flag, out HB32Header header, out byte[] bytes)
        {
            ReceiveBytes(out header, out bytes);
            if (header.Flag != flag)
            {
                throw new SocketFlagException(flag, header, bytes);
            }
        }


        public void ReceiveBytesWithHeaderFlag(PacketType flag)
        {
            ReceiveBytesWithHeaderFlag(flag, out _, out _);
        }

        public void ReceiveBytesWithHeaderFlag(PacketType flag, out HB32Header header)
        {
            ReceiveBytesWithHeaderFlag(flag, out header, out _);
        }


        public void ReceiveBytesWithHeaderFlag(PacketType flag, out byte[] bytes)
        {
            ReceiveBytesWithHeaderFlag(flag, out _, out bytes);
        }

        public static void CheckFlag(PacketType required_flag, HB32Response response)
        {
            if (response.Header.Flag != required_flag)
            {
                throw new SocketFlagException(required_flag, response);
            }
        }

        #endregion


        public void Connect(TCPAddress address)
        {
            IPEndPoint ipe = new IPEndPoint(address.IP, address.Port);
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(ipe);
        }


        private class ConnectTimeoutHandler
        {
            private readonly ManualResetEvent ConnectTimeoutObject = new ManualResetEvent(false);

            public bool IsSuccess { get; set; } = false;

            public Exception ConnectException { get; set; } = new Exception("null connect exception");


            public void Set()
            {
                ConnectTimeoutObject.Set();
            }

            public void Reset()
            {
                ConnectTimeoutObject.Reset();
            }

            public bool WaitOne(int millisecondsTimeout, bool exitContext)
            {
                return ConnectTimeoutObject.WaitOne(millisecondsTimeout, exitContext);
            }


        }

        private readonly ConnectTimeoutHandler cth = new ConnectTimeoutHandler();



        public void ConnectWithTimeout(TCPAddress address, int timeout)
        {
            cth.Reset();
            IPEndPoint ipe = new IPEndPoint(address.IP, address.Port);
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.BeginConnect(ipe, asyncResult =>
            {
                try
                {
                    cth.IsSuccess = false;
                    if (asyncResult.AsyncState is Socket s)
                    {
                        s.EndConnect(asyncResult);
                        cth.IsSuccess = true;
                    }
                }
                catch(Exception ex)
                {
                    cth.IsSuccess = false;
                    cth.ConnectException = ex;
                }
                finally
                {
                    cth.Set();
                }
            }, socket);
            if (cth.WaitOne(timeout, false))
            {
                if (cth.IsSuccess)
                {
                    return;
                }
                else
                {
                    throw cth.ConnectException;
                }

            }
            else
            {
                socket.Close();
                throw new TimeoutException("Connection timeout");
            }

        }



        public void Connect(TCPAddress address, int send_timeout, int recv_timeout)
        {
            IPEndPoint ipe = new IPEndPoint(address.IP, address.Port);
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.SendTimeout = send_timeout;
            socket.ReceiveTimeout = recv_timeout;
            socket.Connect(ipe);
        }

        public virtual void Close()
        {
            CloseSocket();
        }


        public void CloseSocket()
        {
            try
            {
                socket.Close();
            }
            catch { }
        }
    }
}