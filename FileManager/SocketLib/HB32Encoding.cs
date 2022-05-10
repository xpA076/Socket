using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.SocketLib
{
    public class HB32Encoding
    {
        public static readonly int HeaderSize = 32;
        public static readonly int DataSize = 4096;
        public static readonly int PacketSize = HeaderSize + DataSize;

        /// <summary>
        /// (建议为false)
        /// 若为 true, 则收发每个packet(包括最后一个) 均与 HBEncoding.DataSize对齐, 不足则用空字节补齐
        /// 若为 false, 最后一个packet只传输有效bytes
        /// </summary>
        public static readonly bool TransferEmptyBytes = false;

        /// <summary>
        /// 不知道为什么, UseLegacyHeader 为true的情况下, 非本机传输延迟只有几ms
        /// 而为 false 情况下, 传输延迟约100ms
        /// 本机传输不受这个影响 [22.05.10]
        /// </summary>
        public static readonly bool UseLegacyHeader = true;
    }
}
