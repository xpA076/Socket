using FileManager.Models.SocketLib;
using FileManager.Utils.Bytes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.Models.Serializable.Transfer
{
    public class ReleaseFileResponse : ISocketSerializable
    {
        public enum ResponseType : int
        {
            ReleaseSuccess, 
            ReleaseFailed,
        }

        public ResponseType Type { get; set; }

        public string Message { get; set; } = "";

        public static ReleaseFileResponse BuildSuccessResponse()
        {
            return new ReleaseFileResponse()
            {
                Type = ResponseType.ReleaseSuccess,
                Message = ""
            };
        }


        public static ReleaseFileResponse BuildFailedResponse(string msg)
        {
            return new ReleaseFileResponse()
            {
                Type = ResponseType.ReleaseFailed,
                Message = msg
            };
        }


        public static ReleaseFileResponse FromBytes(byte[] bytes, int idx = 0)
        {
            ReleaseFileResponse obj = new ReleaseFileResponse();
            obj.BuildFromBytes(bytes, ref idx);
            return obj;
        }

        public byte[] ToBytes()
        {
            BytesBuilder bb = new BytesBuilder();
            bb.Append((int)Type);
            bb.Append(Message);
            return bb.GetBytes();
        }


        public void BuildFromBytes(byte[] bytes, ref int idx)
        {
            Type = (ResponseType)BytesParser.GetInt(bytes, ref idx);
            Message = BytesParser.GetString(bytes, ref idx);
        }

    }
}
