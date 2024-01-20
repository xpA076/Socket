using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FileManager.Models.SocketLib.Enums;
using FileManager.Utils.Bytes;

namespace FileManager.Models.SocketLib.HbProtocol
{
    /// <summary>
    /// 在数据前用 32Bytes 封装标识数据流 packet 目标, 长度, 位置. 格式
    /// Header 里不应包含数据
    /// </summary>
    public class HB32Header
    {
        public PacketType Flag
        {
            get { return (PacketType)GetInt(0); }
            set { WriteInt(0, (int)value); }
        }
        public int I1
        {
            get { return GetInt(4); }
            set { WriteInt(4, value); }
        }
        public int I2
        {
            get { return GetInt(8); }
            set { WriteInt(8, value); }
        }
        public int I3
        {
            get { return GetInt(12); }
            set { WriteInt(12, value); }
        }
        /// <summary>
        /// Packet counts(all) in Legacy header
        /// </summary>
        public int Default4
        {
            get { return GetInt(16); }
            set { WriteInt(16, value); }
        }
        public int TotalByteLength
        {
            get { return GetInt(20); }
            set { WriteInt(20, value); }
        }
        public int RemainByteLength
        {
            get { return GetInt(24); }
            set { WriteInt(24, value); }
        }
        public int ValidByteLength
        {
            get { return GetInt(28); }
            set { WriteInt(28, value); }
        }

        private byte[] _bytes = new byte[32];

        public HB32Header()
        {

        }

        public HB32Header(byte[] bytes)
        {
            Array.Copy(bytes, _bytes, 32);
        }

        public HB32Header(PacketType flag)
        {
            Flag = flag;
        }


        public byte[] GetBytes(byte[] proxy_header)
        {
            return BytesConverter.WriteIntArray(proxy_header, new int[]
            {
                (int)Flag,
                I1,
                I2,
                I3,
                Default4,
                TotalByteLength,
                RemainByteLength,
                ValidByteLength
            }, proxy_header.Length);
        }


        public byte[] GetBytes()
        {
            return _bytes;
        }


        public static byte[] GetBytes(HB32Header header)
        {
            return header.GetBytes();
        }


        public static HB32Header ReadFromBytes(byte[] bytes, int idx = 0)
        {
            return new HB32Header(bytes.Skip(idx).Take(32).ToArray());
        }

        public HB32Header Copy()
        {
            return new HB32Header(_bytes);
        }

        private void WriteInt(int offset, int val)
        {
            byte[] bs = BitConverter.GetBytes(val);
            _bytes[offset] = bs[0];
            _bytes[offset + 1] = bs[1];
            _bytes[offset + 2] = bs[2];
            _bytes[offset + 3] = bs[3];
        }

        private int GetInt(int offset)
        {
            return BitConverter.ToInt32(_bytes, offset);
        }

        /*
        public void WriteToBytes(byte[] bytes)
        {
            EncodeInt((int)Flag, bytes, 0);
            EncodeInt((int)I1, bytes, 4);
            EncodeInt(I2, bytes, 8);
            EncodeInt(I3, bytes, 12);
            EncodeInt(PacketCount, bytes, 16); 
            EncodeInt(TotalByteLength, bytes, 20);
            EncodeInt(PacketIndex, bytes, 24);
            EncodeInt(ValidByteLength, bytes, 28);
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
         
         */



    }
}
