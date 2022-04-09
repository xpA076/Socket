using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.Models.Serializable
{
    public class SessionRequest : ISocketSerializable
    {
        public enum BytesType : int
        {
            KeyBytes,
            SessionBytes
        }

        public BytesType Type { get; set; }

        public byte[] Bytes { get; set; } = new byte[0];

        public string Name { get; set; } = "";

        public byte[] InfoBytes { get; set; } = new byte[0];


        public static SessionRequest FromBytes(byte[] bytes)
        {
            SessionRequest obj = new SessionRequest();
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
