using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.Models.Serializable
{
    public class DownloadResponse : ISocketSerializable
    {
        public enum ResponseType : int
        {
            BytesResponse,
            PacketResponse,
            ResponseException
        }

        public ResponseType Type { get; set; }

        public byte[] Bytes { get; set; }

        public static DownloadResponse FromBytes(byte[] bytes)
        {
            DownloadResponse response = new DownloadResponse();
            response.BuildFromBytes(bytes);
            return response;
        }

        public void BuildFromBytes(byte[] bytes)
        {
            throw new NotImplementedException();
        }

        public byte[] ToBytes()
        {
            throw new NotImplementedException();
        }
    }
}
