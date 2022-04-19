using FileManager.Exceptions;
using FileManager.SocketLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.Models.Serializable
{
    // TODO 改成 UploadRequest
    public class FileRequest : ISocketSerializable
    {
        public enum RequestType : int
        {
            //FileReadOpen = 0x11,
            //FileReadClose = 0x12,
            FileReadSpan = 0x14,
            FileWriteOpen = 0x21,
            FileWriteClose = 0x22,
            FileWriteSpan = 0x24,
        }

        public RequestType Type { get; set; }

        public string ServerPath { get; set; }

        public long StartPosition { get; set; } = 0;

        public long EndPosition { get; set; } = 0;

        /// <summary>
        /// 上传文件时, 在这里写入 bytes
        /// </summary>
        public byte[] Bytes { get; set; }



        public FileRequest() { }


        /// <summary>
        /// Download 文件时使用
        /// Type = RequestType.FileReadSpan
        /// </summary>
        /// <param name="server_path"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        public FileRequest(string server_path, long start, long end)
        {
            this.Type = RequestType.FileReadSpan;
            this.ServerPath = server_path;
            this.StartPosition = start;
            this.EndPosition = end;
            this.Bytes = new byte[0];
        }


        public static FileRequest FromBytes(byte[] bytes)
        {
            int idx = 0;
            FileRequest obj = new FileRequest();
            obj.BuildFromBytes(bytes, ref idx);
            return obj;
        }


        public void BuildFromBytes(byte[] bytes, ref int idx)
        {
            this.Type = (RequestType)BytesParser.GetInt(bytes, ref idx);
            this.ServerPath = BytesParser.GetString(bytes, ref idx);
            this.StartPosition = BytesParser.GetLong(bytes, ref idx);
            this.EndPosition = BytesParser.GetLong(bytes, ref idx);
            this.Bytes = BytesParser.GetBytes(bytes, ref idx);
        }


        public byte[] ToBytes()
        {
            BytesBuilder bb = new BytesBuilder();
            bb.Append((int)Type);
            bb.Append(ServerPath);
            bb.Append(StartPosition);
            bb.Append(EndPosition);
            bb.Append(Bytes);
            return bb.GetBytes();
        }
    }
}
