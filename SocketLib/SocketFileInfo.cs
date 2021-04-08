using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketLib
{
    public class SocketFileInfo
    {
        public string Name { get; set; }
        public long Length { get; set; } = 0;
        public bool IsDirectory { get; set; } = false;

        [System.Web.Script.Serialization.ScriptIgnore]
        public string Size
        {
            get
            {
                if (IsDirectory) { return ""; }
                if ((Length / (1 << 30)) > 0)
                {
                    double size = (double)(Length >> 20) / 1024;
                    return size.ToString("0.00") + " G";
                }
                else if((Length / (1 << 20)) > 0)
                {
                    double size = (double)(Length >> 10) / 1024;
                    return size.ToString("0.00") + " M";
                }
                else if ((Length / (1 << 10)) > 0)
                {
                    double size = (double)Length / 1024;
                    return size.ToString("0.00") + " K";
                }
                else
                {
                    return Length.ToString() + " B";
                }
            }
        }

        public static int Compare(SocketFileInfo f1, SocketFileInfo f2)
        {
            if (f1.IsDirectory == f2.IsDirectory)
            {
                return f1.Name.CompareTo(f2.Name);
            }
            else
            {
                return f1.IsDirectory ? -1 : 1;
            }
        }

        #region Bytes convertion

        public static byte[] ListToBytes(SocketFileInfo[] socketFileInfos)
        {
            List<byte[]> byteList = new List<byte[]>();
            int bytesCount = 0;
            foreach(SocketFileInfo info in socketFileInfos)
            {
                byte[] _bytes = ToBytes(info);
                byteList.Add(_bytes);
                bytesCount += _bytes.Length;
            }

            byte[] bytes = new byte[bytesCount + 4];
            int idx = 0;
            bytes = BytesParser.WriteInt(bytes, byteList.Count, ref idx);
            foreach(byte[] _bytes in byteList)
            {
                Array.Copy(_bytes, 0, bytes, idx, _bytes.Length);
                idx += _bytes.Length;
            }
            return bytes;
        }

        public static SocketFileInfo[] BytesToList(byte[] bytes)
        {
            int idx = 0;
            int len = BytesParser.ParseInt(bytes, ref idx);
            SocketFileInfo[] socketFileInfos = new SocketFileInfo[len];
            for (int i = 0; i < len; ++i)
            {
                socketFileInfos[i] = FromBytes(bytes, ref idx);
            }
            return socketFileInfos;
        }

        public static byte[] ToBytes(SocketFileInfo info)
        {
            byte[] bytes = new byte[info.Name.Length + 4 + 8 + 1];
            int idx = 0;
            bytes = BytesParser.WriteString(bytes, info.Name, ref idx);
            bytes = BytesParser.WriteLong(bytes, info.Length, ref idx);
            bytes = BytesParser.WriteBool(bytes, info.IsDirectory, ref idx);
            return bytes;
        }

        public static SocketFileInfo FromBytes(byte[] bytes, ref int idx)
        {
            SocketFileInfo info = new SocketFileInfo();
            info.Name = BytesParser.ParseString(bytes, ref idx);
            info.Length = BytesParser.ParseLong(bytes, ref idx);
            info.IsDirectory = BytesParser.ParseBool(bytes, ref idx);
            return info;
        }

        #endregion


    }
}
