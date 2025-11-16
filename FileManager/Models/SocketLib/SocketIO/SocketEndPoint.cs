using FileManager.Exceptions;
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
using FileManager.Models.SocketLib.Enums;
using FileManager.Models.SocketLib.HbProtocol;
using FileManager.Models.SocketLib.Models;
using FileManager.Utils.Bytes;
using Microsoft.VisualBasic;
using System.Reflection.Metadata.Ecma335;


namespace FileManager.Models.SocketLib.SocketIO
{
    /// <summary>
    /// Socket 连接中的 client / server 端, 有简单封装的 Connect / SendBytes / ReceiveBytes 方法
    /// </summary>

    public class SocketEndPoint : IDisposable
    {
        

        private const int BufferSize = 64 * 1024;

        private const uint MagicHeader = 0x0134DA75;

        protected Socket? socket;

        public bool Connected
        {
            get
            {
                return this.socket.Connected;
            }
        }

        public SocketEndPoint()
        {

        }

        protected byte[]? AesKeys;

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
            int rec = 0;    /// 函数内累计接收字节数
            int _rec;       /// 单个 Socket.Receive 调用接收字节数
            while (rec != _size)
            {
                _rec = socket.Receive(buffer, offset + rec, _size - rec, SocketFlags.None);
                if (_rec == 0) throw new SocketConnectionException(SocketStatus.ZeroReceive);
                rec += _rec;
            }
        }

        private async Task ReceiveBufferAsync(byte[] buffer, int size = -1, int offset = 0)
        {
            if (buffer.Length == 0) { return; }
            int _size = (size < 0) ? buffer.Length : size;
            int rec = 0;    /// 函数内累计接收字节数
            while (rec < _size)
            {
                int _rec = await socket.ReceiveAsync(new ArraySegment<byte>(buffer, rec, size - rec), SocketFlags.None);
                if (_rec == 0) throw new SocketConnectionException(SocketStatus.ZeroReceive);
                rec += _rec;
            }
        }


        private async Task SendBytesIOAsync(byte[] bytes, bool encrypted = false)
        {
            /// Send Header
            CRC32 crc32 = new CRC32();
            uint result = crc32.Compute(bytes);
            BytesBuilder bb = new BytesBuilder();
            bb.Append(SocketEndPoint.MagicHeader);
            bb.Append((uint)bytes.Length);
            bb.Append(result);
            bb.Append(encrypted);
            bb.Append(new byte[3]);
            await socket.SendAsync(new ArraySegment<byte>(bb.GetBytes()), SocketFlags.None);

            /// Send content
            int totalSent = 0;
            int remaining = bytes.Length;
            while (true)
            {
                /// send trunk
                int chunkSize = Math.Min(BufferSize, remaining);
                var segment = new ArraySegment<byte>(bytes, totalSent, chunkSize);
                int sent = await socket.SendAsync(segment, SocketFlags.None);
                if (sent == 0) throw new SocketConnectionException(SocketStatus.ZeroSend);
                totalSent += sent;
                remaining -= sent;
                /// break
                if (remaining == 0) break;
                /// send magic header
                sent = await socket.SendAsync(BitConverter.GetBytes(SocketEndPoint.MagicHeader), SocketFlags.None);
                if (sent == 0) throw new SocketConnectionException(SocketStatus.ZeroSend);
            }
        }

        private void SendBytesIO(byte[] bytes, bool encrypted = false)
        {
            /// Send Header
            CRC32 crc32 = new CRC32();
            uint result = crc32.Compute(bytes);
            BytesBuilder bb = new BytesBuilder();
            bb.Append(SocketEndPoint.MagicHeader);
            bb.Append((uint)bytes.Length);
            bb.Append(result);
            bb.Append(encrypted);
            bb.Append(new byte[3]);
            socket.Send(new ArraySegment<byte>(bb.GetBytes()), SocketFlags.None);

            /// Send content
            int totalSent = 0;
            int remaining = bytes.Length;
            while (true)
            {
                /// send trunk
                int chunkSize = Math.Min(BufferSize, remaining);
                var segment = new ArraySegment<byte>(bytes, totalSent, chunkSize);
                int sent = socket.Send(segment, SocketFlags.None);
                if (sent == 0) throw new SocketConnectionException(SocketStatus.ZeroSend);
                totalSent += sent;
                remaining -= sent;
                /// break
                if (remaining == 0) break;
                /// send magic header
                sent = socket.Send(BitConverter.GetBytes(SocketEndPoint.MagicHeader), SocketFlags.None);
                if (sent == 0) throw new SocketConnectionException(SocketStatus.ZeroSend);
            }
        }

        private async Task<(byte[], bool)> ReceiveBytesIOAsync()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                /// Receive header
                byte[] header = new byte[16];
                await this.ReceiveBufferAsync(header);
                uint receiveMagicHeader = BitConverter.ToUInt32(header, 0);
                int totalLength = (int)BitConverter.ToUInt32(header, 4);
                uint checkSum = BitConverter.ToUInt32(header, 8);
                bool encrypted = header[12] != 0;
                if (receiveMagicHeader != SocketEndPoint.MagicHeader) throw new SocketConnectionException(SocketStatus.WrongMagicHeader);

                /// Receive content
                byte[] buffer = new byte[SocketEndPoint.BufferSize];
                int totalReceived = 0;
                while (true)
                {
                    /// receive content
                    int toReceive = (int)Math.Min(buffer.Length, totalLength - totalReceived);
                    int received = await socket.ReceiveAsync(new ArraySegment<byte>(buffer, 0, toReceive), SocketFlags.None);
                    if (received == 0) throw new SocketConnectionException(SocketStatus.ZeroReceive);
                    await ms.WriteAsync(buffer, 0, received);
                    totalReceived += received;
                    /// break
                    if (totalReceived == totalLength) break;
                    /// receive magic header after trunk
                    byte[] recv = new byte[4];
                    await ReceiveBufferAsync(recv);
                    receiveMagicHeader = BitConverter.ToUInt32(recv, 0);
                    if (receiveMagicHeader != SocketEndPoint.MagicHeader) throw new SocketConnectionException(SocketStatus.WrongMagicHeader);
                }

                /// Check sum
                byte[] bytes = ms.ToArray();
                CRC32 crc32 = new CRC32();
                uint crc_result = crc32.Compute(bytes);
                if (crc_result != checkSum) throw new SocketConnectionException(SocketStatus.WrongCheckSum);

                /// return
                return (bytes, encrypted);
            }
        }

        private (byte[] bytes, bool encrypted) ReceiveBytesIO()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                /// Receive header
                byte[] header = new byte[16];
                this.ReceiveBuffer(header);
                uint receiveMagicHeader = BitConverter.ToUInt32(header, 0);
                int totalLength = (int)BitConverter.ToUInt32(header, 4);
                uint checkSum = BitConverter.ToUInt32(header, 8);
                bool encrypted = header[12] != 0;
                if (receiveMagicHeader != SocketEndPoint.MagicHeader) throw new SocketConnectionException(SocketStatus.WrongMagicHeader);

                /// Receive content
                byte[] buffer = new byte[SocketEndPoint.BufferSize];
                int totalReceived = 0;
                while (true)
                {
                    /// receive content
                    int toReceive = (int)Math.Min(buffer.Length, totalLength - totalReceived);
                    int received = socket.Receive(new ArraySegment<byte>(buffer, 0, toReceive), SocketFlags.None);
                    if (received == 0) throw new SocketConnectionException(SocketStatus.ZeroReceive);
                    ms.Write(buffer, 0, received);
                    totalReceived += received;
                    /// break
                    if (totalReceived == totalLength) break;
                    /// receive magic header after trunk
                    byte[] recv = new byte[4];
                    this.ReceiveBuffer(recv);
                    receiveMagicHeader = BitConverter.ToUInt32(recv, 0);
                    if (receiveMagicHeader != SocketEndPoint.MagicHeader) throw new SocketConnectionException(SocketStatus.WrongMagicHeader);
                }

                /// Check sum
                byte[] bytes = ms.ToArray();
                CRC32 crc32 = new CRC32();
                uint crc_result = crc32.Compute(bytes);
                if (crc_result != checkSum) throw new SocketConnectionException(SocketStatus.WrongCheckSum);

                /// return
                return (bytes, encrypted);
            }
        }

        public async Task SendBytesAsync(byte[] bytes)
        {
            if (this.AesKeys != null)
            {
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
                await SendBytesIOAsync(enc.ToBytes(), true);
            }
            else
            {
                await SendBytesIOAsync(bytes, false);
            }
        }

        public void SendBytes(byte[] bytes)
        {
            if (this.AesKeys != null)
            {
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
                SendBytesIO(enc.ToBytes(), true);
            }
            else
            {
                SendBytesIO(bytes, false);
            }
        }

        public async Task<byte[]> ReceiveBytesAsync()
        {
            var (bytes, encrypted) = await ReceiveBytesIOAsync();
            if (encrypted)
            {
                if (this.AesKeys == null) throw new SocketConnectionException(SocketStatus.DecryptExcepton, "Need decrpypt before AES key setup");
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
            else
            {
                return bytes;
            }
        }

        public byte[] ReceiveBytes()
        {
            var (bytes, encrypted) = ReceiveBytesIO();
            if (encrypted)
            {
                if (this.AesKeys == null) throw new SocketConnectionException(SocketStatus.DecryptExcepton, "Need decrpypt before AES key setup");
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
            else
            {
                return bytes;
            }
        }
        /*

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
        */
        #endregion

        #region Send / Receive


        public void SendHeader(HB32Header header)
        {
            SocketIO.SendHeader(socket, header, new byte[2] { 0, 0 });

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
            SocketIO.SendBytes(socket, new byte[2] { 0, 0 }, header, bytes, packetLength);
        }

        public void SendBytes(HB32Header header, byte[] bytes)
        {
            SendBytes(header, bytes, HB32Encoding.DataSize);
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



        public void ReceiveBytes(out HB32Header header, out byte[] bytes)
        {

            SocketIO.ReceiveBytes(socket, out header, out bytes);
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



        #endregion

        /*

        public void Connect(TCPAddress address)
        {
            IPEndPoint ipe = new IPEndPoint(address.IP, address.Port);
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(ipe);
        }
        */




        public void Shutdown()
        {
            this.socket.Shutdown(SocketShutdown.Both);
        }

        public void Dispose()
        {
            if (socket == null) return;
            try
            {
                if (socket.Connected)
                {
                    socket.Shutdown(SocketShutdown.Both);
                }
            }
            catch (SocketException)
            {

            }
            finally
            {
                socket.Close();
                socket.Dispose();
            }
        }
    }
}