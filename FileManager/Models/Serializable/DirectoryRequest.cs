using FileManager.Models.SocketLib;
using FileManager.Utils.Bytes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.Models.Serializable
{
    public class DirectoryRequest : ISocketSerializable
    {
        public enum RequestType : int
        {
            Query,
            CreateDirectory,
        }

        public RequestType Type { get; set; }

        public string ServerPath { get; set; }

        public DirectoryRequest()
        {

        }

        public DirectoryRequest(string server_path)
        {
            ServerPath = server_path;
        }

        public static DirectoryRequest FromBytes(byte[] bytes, int idx = 0)
        {
            DirectoryRequest obj = new DirectoryRequest();
            obj.BuildFromBytes(bytes, ref idx);
            return obj;
        }

        public byte[] ToBytes()
        {
            BytesBuilder bb = new BytesBuilder();
            bb.Append((int)Type);
            bb.Append(ServerPath);
            return bb.GetBytes();
        }

        public void BuildFromBytes(byte[] bytes, ref int idx)
        {
            this.Type = (RequestType)BytesParser.GetInt(bytes, ref idx);
            this.ServerPath = BytesParser.GetString(bytes, ref idx);
        }
    }
}
