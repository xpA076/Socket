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

        public static SessionResponse FromBytes(byte[] bytes)
        {
            SessionResponse obj = new SessionResponse();
            obj.BuildFromBytes(bytes);
            return obj;
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
