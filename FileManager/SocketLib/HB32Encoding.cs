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

        /*
        public static byte[] GetBytes(HB32Header header, string s)
        {
            byte[] bytes = new byte[PacketSize];

            byte[] bytes_str = Encoding.UTF8.GetBytes(s);
            if (bytes_str.Length > PacketSize - 32)
            {
                throw new ArgumentException("not enough capacity for string");
            }
            header.ValidByteLength = bytes_str.Length;
            header.WriteToBytes(bytes);

            for (int i = 0; i < bytes_str.Length; ++i)
            {
                bytes[i + 32] = bytes_str[i];
            }
            return bytes;
        }

        public static string GetString(byte[] bytes)
        {
            HB32Header header = HB32Header.ReadFromBytes(bytes);
            return Encoding.UTF8.GetString(bytes, 32, header.ValidByteLength);
        }
        */
    }
}
