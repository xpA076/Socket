using FileManager.SocketLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.Models.Serializable
{
    public class DownloadRequest : ISocketSerializable
    {
        public enum RequestType : int
        {
            QueryByPath,
            //QueryByDictionary,
        }

        public RequestType Type { get; set; }

        public string ViewPath { get; set; }

        public long Begin { get; set; }

        public long Length { get; set; }


        public static DownloadRequest FromBytes(byte[] bytes)
        {
            int idx = 0;
            DownloadRequest obj = new DownloadRequest();
            obj.BuildFromBytes(bytes, ref idx);
            return obj;
        }

        public byte[] ToBytes()
        {
            BytesBuilder bb = new BytesBuilder();
            bb.Append((int)Type);
            bb.Append(ViewPath);
            bb.Append(Begin);
            bb.Append(Length);
            return bb.GetBytes();
        }

        public void BuildFromBytes(byte[] bytes, ref int idx)
        {
            this.Type = (RequestType)BytesParser.GetInt(bytes, ref idx);
            this.ViewPath = BytesParser.GetString(bytes, ref idx);
            this.Begin = BytesParser.GetLong(bytes, ref idx);
            this.Length = BytesParser.GetLong(bytes, ref idx);
        }
    }
}
