using FileManager.Exceptions;
using FileManager.Models.SocketLib;
using FileManager.Utils.Bytes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.Models.Serializable.Transfer
{
    public class UploadResponse : ISocketSerializable
    {
        public enum ResponseType : int
        {
            SuccessResponse, 
            ResponseException
        }

        public ResponseType Type { get; set; }

        public byte[] Bytes { get; set; }

        public string ExceptionMessage
        {
            get
            {
                if (Type == ResponseType.ResponseException)
                {
                    return Encoding.UTF8.GetString(Bytes);
                }
                else
                {
                    throw new SocketTypeException(ResponseType.ResponseException, Type);
                }
            }
        }

        private UploadResponse()
        {

        }


        public UploadResponse(string err_msg)
        {
            Type = ResponseType.ResponseException;
            Bytes = Encoding.UTF8.GetBytes(err_msg);
        }

        public static UploadResponse BuildSuccessResponse()
        {
            UploadResponse obj = new UploadResponse();
            obj.Type = ResponseType.SuccessResponse;
            obj.Bytes = new byte[0];
            return obj;
        }

        public static UploadResponse FromBytes(byte[] bytes, int idx = 0)
        {
            UploadResponse response = new UploadResponse();
            response.BuildFromBytes(bytes, ref idx);
            return response;
        }


        public byte[] ToBytes()
        {
            BytesBuilder bb = new BytesBuilder();
            bb.Append((int)Type);
            bb.Append(Bytes);
            return bb.GetBytes();
        }

        public void BuildFromBytes(byte[] bytes, ref int idx)
        {
            Type = (ResponseType)BytesParser.GetInt(bytes, ref idx);
            Bytes = BytesParser.GetBytes(bytes, ref idx);
        }

    }
}
