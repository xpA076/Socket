using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketLib
{
    public class HB32Header
    {
        public SocketDataFlag Flag { get; set; } = 0;
        public int I1 { get; set; } = 0;
        public int I2 { get; set; } = 0;
        public int I3 { get; set; } = 0;
        public int PackageCount { get; set; } = 1;
        public int TotalByteLength { get; set; } = 0;
        public int PackageIndex { get; set; } = 0;
        public int ValidByteLength { get; set; } = 0;

        public void WriteToBytes(byte[] bytes)
        {
            EncodeInt((int)Flag, bytes, 0);
            EncodeInt((int)I1, bytes, 4);
            EncodeInt(I2, bytes, 8);
            EncodeInt(I3, bytes, 12);
            EncodeInt(PackageCount, bytes, 16);
            EncodeInt(TotalByteLength, bytes, 20);
            EncodeInt(PackageIndex, bytes, 24);
            EncodeInt(ValidByteLength, bytes, 28);
        }

        public byte[] GetBytes()
        {
            byte[] bytes = new byte[HB32Encoding.HeaderSize];
            this.WriteToBytes(bytes);
            return bytes;
        }

        public static HB32Header ReadFromBytes(byte[] bytes)
        {
            return new HB32Header()
            {
                Flag = (SocketDataFlag)DecodeInt(bytes, 0),
                I1 = DecodeInt(bytes, 4),
                I2 = DecodeInt(bytes, 8),
                I3 = DecodeInt(bytes, 12),
                PackageCount = DecodeInt(bytes, 16),
                TotalByteLength = DecodeInt(bytes, 20),
                PackageIndex = DecodeInt(bytes, 24),
                ValidByteLength = DecodeInt(bytes, 28),
            };
        }

        public static void EncodeInt(int i, byte[] data, int index)
        {
            data[index] = (byte)(i & 0xFF);
            data[index + 1] = (byte)((i >> 8) & 0xFF);
            data[index + 2] = (byte)((i >> 16) & 0xFF);
            data[index + 3] = (byte)((i >> 24) & 0xFF);
        }

        public static int DecodeInt(byte[] data, int index)
        {
            return (data[index] & 0xFF) | ((data[index + 1] & 0xFF) << 8) |
                ((data[index + 2] & 0xFF) << 16) | ((data[index + 3] & 0xFF) << 24);

        }

    }
}
