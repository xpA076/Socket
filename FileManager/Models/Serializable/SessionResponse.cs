using FileManager.Exceptions;
using FileManager.SocketLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.Models.Serializable
{
    public class SessionResponse : ISocketSerializable
    {
        public enum ResponseType : int
        {
            NoModify,
            NewSessionBytes,
            SessionException
        }

        public ResponseType Type { get; set; }

        public byte[] Bytes { get; set; }

        public string ExceptionMessage
        {
            get
            {
                if (Type == ResponseType.SessionException)
                {
                    return Encoding.UTF8.GetString(Bytes);
                }
                else
                {
                    throw new SocketTypeException(ResponseType.SessionException, Type);
                }
            }
        }

        public static SessionResponse FromBytes(byte[] bytes, int idx = 0)
        {
            SessionResponse obj = new SessionResponse();
            obj.BuildFromBytes(bytes, ref idx);
            return obj;
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
            this.Type = (ResponseType)BytesParser.GetInt(bytes, ref idx);
            this.Bytes = BytesParser.GetBytes(bytes, ref idx);
        }
    }
}
