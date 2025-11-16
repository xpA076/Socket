using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.Utils.Bytes
{
    public class CRC32
    {
        private readonly uint[] table;
        private const uint Polynomial = 0xEDB88320;

        public CRC32()
        {
            table = new uint[256];
            for (uint i = 0; i < 256; i++)
            {
                uint entry = i;
                for (int j = 0; j < 8; j++)
                {
                    if ((entry & 1) == 1)
                        entry = (entry >> 1) ^ Polynomial;
                    else
                        entry >>= 1;
                }
                table[i] = entry;
            }
        }

        public uint Compute(byte[] data)
        {
            uint crc = 0xFFFFFFFF;
            foreach (byte b in data)
            {
                byte index = (byte)((crc & 0xFF) ^ b);
                crc = (crc >> 8) ^ table[index];
            }
            return ~crc;
        }

        public uint Compute(string text)
        {
            byte[] data = Encoding.UTF8.GetBytes(text);
            return Compute(data);
        }
    }
}
