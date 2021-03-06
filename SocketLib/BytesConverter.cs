﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketLib
{
    public static class BytesConverter
    {
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="index">byte解码起始位置</param>
        /// <param name="count">byte解码长度</param>
        /// <returns></returns>
        public static int[] ParseIntArray(byte[] bytes, int index, int count)
        {
            int[] array = new int[count / 4];
            int start = index;
            for (int int_idx = 0; int_idx < count / 4; ++int_idx)
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
