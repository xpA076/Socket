using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SocketLib.Enums;

namespace SocketLib
{

    public delegate byte[] GetHeaderBytesHandler(HB32Header header);

    public class HB32Header
    {
        public SocketPacketFlag Flag { get; set; } = 0;
        public int I1 { get; set; } = 0;
        public int I2 { get; set; } = 0;
        public int I3 { get; set; } = 0;
        public int PacketCount { get; set; } = 1;
        public int TotalByteLength { get; set; } = 0;
        public int PacketIndex { get; set; } = 0;
        public int ValidByteLength { get; set; } = 0;

        public byte[] GetBytes()
        {
            return BytesConverter.WriteIntArray(new int[] 
            { 
                (int)Flag,
                I1,
                I2, 
                I3,
                PacketCount,
                TotalByteLength,
                PacketIndex,
                ValidByteLength
            });
        }


        public static byte[] GetBytes(HB32Header header)
        {
            return header.GetBytes();
        }


        public static HB32Header ReadFromBytes(byte[] bytes)
        {
            int[] array = BytesConverter.ParseIntArray(bytes, 0, 32);
            return new HB32Header()
            {
                Flag = (SocketPacketFlag)array[0],
                I1 = array[1],
                I2 = array[2],
                I3 = array[3],
                PacketCount = array[4],
                TotalByteLength = array[5],
                PacketIndex = array[6],
                ValidByteLength = array[7],
            };
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
