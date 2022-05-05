using FileManager.SocketLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.Models.Serializable
{
    public class UploadRequest: ISocketSerializable
    {
        public enum RequestType : int
        {
            ByPath,
        }

        public RequestType Type { get; set; }

        public string ViewPath { get; set; }

        public long Begin { get; set; }

        public long Length { get; set; }

        public byte[] Bytes { get; set; }


        public static UploadRequest FromBytes(byte[] bytes)
        {
            int idx = 0;
            UploadRequest obj = new UploadRequest();
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
            bb.Append(Bytes);
            return bb.GetBytes();
        }

        public void BuildFromBytes(byte[] bytes, ref int idx)
        {
            this.Type = (RequestType)BytesParser.GetInt(bytes, ref idx);
            this.ViewPath = BytesParser.GetString(bytes, ref idx);
            this.Begin = BytesParser.GetLong(bytes, ref idx);
            this.Length = BytesParser.GetLong(bytes, ref idx);
            this.Bytes = BytesParser.GetBytes(bytes, ref idx);
        }

    }
}
