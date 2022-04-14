using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FileManager.SocketLib;

namespace FileManager.Models.Serializable
{
    public class SocketFileInfo : ISocketSerializable
    {
        public string Name { get; set; } = "";
        public bool IsDirectory { get; set; } = false;
        public long Length { get; set; } = 0;
        public DateTime CreationTimeUtc { get; set; } = new DateTime(0);
        public DateTime LastWriteTimeUtc { get; set; } = new DateTime(0);


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

        public SocketFileInfo Copy()
        {
            return new SocketFileInfo
            {
                Name = this.Name,
                IsDirectory = this.IsDirectory,
                Length = this.Length,
                CreationTimeUtc = this.CreationTimeUtc,
                LastWriteTimeUtc = this.LastWriteTimeUtc
            };
        }


        /// <summary>
        /// [4-byte List长度] + (List元素bytes)*n
        /// </summary>
        /// <param name="socketFileInfos"></param>
        /// <returns></returns>
        public static byte[] ListToBytes(List<SocketFileInfo> socketFileInfos)
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
            Array.Copy(BitConverter.GetBytes(byteList.Count), bytes, 4);
            int idx = 4;
            foreach (byte[] _bytes in byteList)
            {
                Array.Copy(_bytes, 0, bytes, idx, _bytes.Length);
                idx += _bytes.Length;
            }
            return bytes;
        }

        public static List<SocketFileInfo> BytesToList(byte[] bytes)
        {
            int len = BitConverter.ToInt32(bytes, 0);
            int idx = 4;
            List<SocketFileInfo> socketFileInfos = new List<SocketFileInfo>();
            for (int i = 0; i < len; ++i)
            {
                socketFileInfos.Add(FromBytes(bytes, ref idx));
            }
            return socketFileInfos;
        }

        public static byte[] ToBytes(SocketFileInfo info)
        {
            BytesBuilder bb = new BytesBuilder();
            bb.Append(info.Name);
            bb.Append(info.IsDirectory);
            bb.Append(info.Length);
            bb.Append(info.CreationTimeUtc);
            bb.Append(info.LastWriteTimeUtc);
            return bb.GetBytes();
        }

        public static SocketFileInfo FromBytes(byte[] bytes, ref int idx)
        {
            throw new NotImplementedException();
            SocketFileInfo info = new SocketFileInfo();
            info.Name = BytesParser.GetString(bytes, ref idx);
            info.IsDirectory = BytesParser.GetBool(bytes, ref idx);
            info.Length = BytesParser.GetLong(bytes, ref idx);
            info.CreationTimeUtc = BytesParser.GetDateTime(bytes, ref idx);
            info.LastWriteTimeUtc = BytesParser.GetDateTime(bytes, ref idx);
            return info;
        }

        public byte[] ToBytes()
        {
            BytesBuilder bb = new BytesBuilder();
            bb.Append(this.Name);
            bb.Append(this.IsDirectory);
            bb.Append(this.Length);
            bb.Append(this.CreationTimeUtc);
            bb.Append(this.LastWriteTimeUtc);
            return bb.GetBytes();
        }

        public void BuildFromBytes(byte[] bytes, ref int idx)
        {
            this.Name = BytesParser.GetString(bytes, ref idx);
            this.IsDirectory = BytesParser.GetBool(bytes, ref idx);
            this.Length = BytesParser.GetLong(bytes, ref idx);
            this.CreationTimeUtc = BytesParser.GetDateTime(bytes, ref idx);
            this.LastWriteTimeUtc = BytesParser.GetDateTime(bytes, ref idx);
        }


    }
}
