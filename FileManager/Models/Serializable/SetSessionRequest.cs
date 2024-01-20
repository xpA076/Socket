using FileManager.Models.SocketLib;
using FileManager.Utils.Bytes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.Models.Serializable
{
    public class SetSessionRequest : ISocketSerializable
    {
        public enum RequestType : int
        {
            FileReadOpen,   /// 此时 Bytes 为 ServerPath 对应的 UTF-8 bytes
            FileReadClose,
        }

        public RequestType Type { get; set; }

        public byte[] Bytes { get; set; }

        private SetSessionRequest()
        {

        }

        public static SetSessionRequest BuildFileReadOpen(string server_path)
        {
            SetSessionRequest request = new SetSessionRequest();
            request.Type = RequestType.FileReadOpen;
            request.Bytes = Encoding.UTF8.GetBytes(server_path);
            return request;
        }

        public static SetSessionRequest FromBytes(byte[] bytes)
        {
            int idx = 0;
            SetSessionRequest obj = new SetSessionRequest();
            obj.BuildFromBytes(bytes, ref idx);
            return obj;
        }


        public void BuildFromBytes(byte[] bytes, ref int idx)
        {
            this.Type = (RequestType)BytesParser.GetInt(bytes, ref idx);
            this.Bytes = BytesParser.GetBytes(bytes, ref idx);
        }

        public byte[] ToBytes()
        {
            BytesBuilder bb = new BytesBuilder();
            bb.Append((int)Type);
            bb.Append(Bytes);
            return bb.GetBytes();
        }
    }
}
