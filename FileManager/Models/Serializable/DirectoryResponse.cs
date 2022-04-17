using FileManager.Exceptions;
using FileManager.SocketLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.Models.Serializable
{
    public class DirectoryResponse : ISocketSerializable
    {
        public enum ResponseType : int
        {
            ListResponse,
            ResponseException
        }

        public ResponseType Type { get; set; }

        public List<SocketFileInfo> FileInfos { get; set; }

        public byte[] AdditionalBytes { get; set; }

        public string ExceptionMessage
        {
            get
            {
                if (Type == ResponseType.ResponseException)
                {
                    return Encoding.UTF8.GetString(AdditionalBytes);
                }
                else
                {
                    throw new SocketTypeException(ResponseType.ResponseException, Type);
                }
            }
        }

        private DirectoryResponse()
        {

        }

        public DirectoryResponse(string err_msg)
        {
            this.Type = ResponseType.ResponseException;
            this.FileInfos = new List<SocketFileInfo>();
            this.AdditionalBytes = Encoding.UTF8.GetBytes(err_msg);
        }

        public DirectoryResponse(List<SocketFileInfo> fileInfos)
        {
            this.Type = ResponseType.ListResponse;
            this.FileInfos = fileInfos;
            this.AdditionalBytes = new byte[0];
        }


        public static DirectoryResponse FromBytes(byte[] bytes)
        {
            int idx = 0;
            DirectoryResponse obj = new DirectoryResponse();
            obj.BuildFromBytes(bytes, ref idx);
            return obj;
        }

        public void BuildFromBytes(byte[] bytes, ref int idx)
        {
            this.Type = (ResponseType)BytesParser.GetInt(bytes, ref idx);
            this.FileInfos = BytesParser.GetListSerializable<SocketFileInfo>(bytes, ref idx);
            this.AdditionalBytes = BytesParser.GetBytes(bytes, ref idx);
        }

        public byte[] ToBytes()
        {
            BytesBuilder bb = new BytesBuilder();
            bb.Append((int)Type);
            bb.Append<SocketFileInfo>(FileInfos);
            bb.Append(AdditionalBytes);
            return bb.GetBytes();
        }
    }
}
