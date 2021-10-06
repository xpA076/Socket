﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.SocketLib
{
    public class BytesParser
    {
        public static bool GetBool(byte[] value, ref int startIndex)
        {
            bool b = value[startIndex] != 0;
            startIndex += 1;
            return b;
        }

        public static int GetInt(byte[] value, ref int startIndex)
        {
            int l = BitConverter.ToInt32(value, startIndex);
            startIndex += 4;
            return l;
        }


        public static long GetLong(byte[] value, ref int startIndex)
        {
            long l = BitConverter.ToInt64(value, startIndex);
            startIndex += 8;
            return l;
        }


        public static string GetString(byte[] value, ref int startIndex)
        {
            int len = BitConverter.ToInt32(value, startIndex);
            string s = Encoding.UTF8.GetString(value, startIndex + 4, len);
            startIndex += 4 + len;
            return s;
        }

        public static DateTime GetDateTime(byte[] value, ref int startIndex)
        {
            DateTime dt = new DateTime(BitConverter.ToInt64(value, startIndex));
            startIndex += 8;
            return dt;
        }

    }
}
