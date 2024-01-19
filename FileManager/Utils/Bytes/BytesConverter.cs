using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.Utils.Bytes
{
    /// <summary>
    /// 这个争取不用
    /// </summary>
    public static class BytesConverter
    {
        public static byte[] Concatenate(byte[] bytes1, byte[] bytes2)
        {
            byte[] bytes = new byte[bytes1.Length + bytes2.Length];
            Array.Copy(bytes1, 0, bytes, 0, bytes1.Length);
            Array.Copy(bytes2, bytes1.Length, bytes, 0, bytes2.Length);
            return bytes;
        }

        #region int

        public static byte[] WriteInt(byte[] bytes, int num, ref int idx)
        {
            byte[] _bytes;
            if (bytes.Length < idx + 4)
            {
                _bytes = new byte[idx + 4];
                Array.Copy(bytes, 0, _bytes, 0, bytes.Length);
            }
            else
            {
                _bytes = bytes;
            }
            for (int i = 0; i < 4; ++i)
            {
                //_bytes[idx + i] = (byte)(num / (1 << (8 * i)) % (1 << 8));
                _bytes[idx + i] = (byte)((num >> (8 * i)) & 0xFF);
            }
            idx += 4;
            return _bytes;
        }


        public static int ParseInt(byte[] bytes, ref int idx)
        {
            int num = 0;
            for (int i = 0; i < 4; ++i)
            {
                num += ((int)bytes[idx + i]) << (8 * i);
            }
            idx += 4;
            return num;
        }


        public static byte[] WriteIntArray(int[] array)
        {
            byte[] _bytes = new byte[array.Length * 4];
            int idx = 0;
            foreach (int i in array)
            {
                WriteInt(_bytes, i, ref idx);
            }
            return _bytes;
        }


        public static byte[] WriteIntArray(byte[] bytes, int[] array, int idx)
        {
            return WriteIntArray(bytes, array, ref idx);
        }


        public static byte[] WriteIntArray(byte[] bytes, int[] array, ref int idx)
        {
            byte[] _bytes;
            if (bytes.Length < idx + array.Length * 4)
            {
                _bytes = new byte[idx + array.Length * 4];
                Array.Copy(bytes, 0, _bytes, 0, bytes.Length);
            }
            else
            {
                _bytes = bytes;
            }
            foreach (int i in array)
            {
                WriteInt(_bytes, i, ref idx);
            }
            return _bytes;
        }


        /// <summary>
        /// 从字节流指定位置开始解码int数组
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="index">byte解码起始位置</param>
        /// <param name="byte_count">byte解码长度</param>
        /// <returns></returns>
        public static int[] ParseIntArray(byte[] bytes, int index, int byte_count)
        {
            int[] array = new int[byte_count / 4];
            int start = index;
            for (int int_idx = 0; int_idx < byte_count / 4; ++int_idx)
            {
                array[int_idx] = ParseInt(bytes, ref start);
            }
            return array;
        }


        #endregion

        public static byte[] WriteLong(byte[] bytes, long num, ref int idx)
        {
            byte[] _bytes;
            if (bytes.Length < idx + 8)
            {
                _bytes = new byte[idx + 8];
                Array.Copy(bytes, 0, _bytes, 0, bytes.Length);
            }
            else
            {
                _bytes = bytes;
            }
            for (int i = 0; i < 8; ++i)
            {
                _bytes[idx + i] = (byte)(num / (1L << (8 * i)) % (1 << 8));
            }
            idx += 8;
            return _bytes;
        }

        public static long ParseLong(byte[] bytes, ref int idx)
        {
            long num = 0;
            for (int i = 0; i < 8; ++i)
            {
                num += ((long)bytes[idx + i]) << (8 * i);
            }
            idx += 8;
            return num;
        }


        public static byte[] WriteString(byte[] bytes, string str, int idx)
        {
            return WriteString(bytes, str, ref idx);
        }


        public static byte[] WriteString(byte[] bytes, string str, ref int idx)
        {
            byte[] strBytes = Encoding.UTF8.GetBytes(str);
            byte[] _bytes;
            if (bytes.Length < idx + 4 + strBytes.Length)
            {
                _bytes = new byte[idx + 4 + strBytes.Length];
                Array.Copy(bytes, 0, _bytes, 0, bytes.Length);
            }
            else
            {
                _bytes = bytes;
            }
            _bytes = WriteInt(_bytes, strBytes.Length, ref idx);
            Array.Copy(strBytes, 0, _bytes, idx, strBytes.Length);
            idx += strBytes.Length;
            return _bytes;
        }

        public static string ParseString(byte[] bytes, ref int idx)
        {
            int len = ParseInt(bytes, ref idx);
            string s = Encoding.UTF8.GetString(bytes, idx, len);
            idx += len;
            return s;
        }

        public static byte[] WriteBool(byte[] bytes, bool flag, ref int idx)
        {
            byte[] _bytes;
            if (bytes.Length < idx + 1)
            {
                _bytes = new byte[idx + 1];
                Array.Copy(bytes, 0, _bytes, 0, bytes.Length);
            }
            else
            {
                _bytes = bytes;
            }
            _bytes[idx] = flag ? (byte)1 : (byte)0;
            idx += 1;
            return _bytes;
        }

        public static bool ParseBool(byte[] bytes, ref int idx)
        {
            bool flag = bytes[idx] == 1;
            idx += 1;
            return flag;
        }


    }
}
