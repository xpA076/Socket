using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketFileManager.SocketLib
{
    public class HB32Encoding
    {
        public static readonly int BufferSize = 4128;
        public static readonly int HeaderSize = 32;
        public static readonly int DataSize = BufferSize - HeaderSize;

        public static byte[] GetBytes(HB32Header header, string s)
        {
            byte[] bytes = new byte[BufferSize];

            byte[] bytes_str = Encoding.UTF8.GetBytes(s);
            if (bytes_str.Length > BufferSize - 32)
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
    }
}
