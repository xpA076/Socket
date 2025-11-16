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
    public class DownloadResponse : ISocketSerializable
    {
        public enum ResponseType : int
        {
            BytesResponse,
            ResponseException,
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

        private DownloadResponse()
        {

        }

        public DownloadResponse(byte[] bytes)
        {
            Type = ResponseType.BytesResponse;
            Bytes = bytes;
        }

        public DownloadResponse(string err_msg)
        {
            Type = ResponseType.ResponseException;
            Bytes = Encoding.UTF8.GetBytes(err_msg);
        }

        public static DownloadResponse FromBytes(byte[] bytes, int idx = 0)
        {
            DownloadResponse response = new DownloadResponse();
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
