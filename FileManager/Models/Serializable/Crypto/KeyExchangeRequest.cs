using FileManager.SocketLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.Models.Serializable.Crypto
{
    public class KeyExchangeRequest : ISocketSerializable
    {
        public byte[] PublicKey { get; set; }

        public static KeyExchangeRequest FromBytes(byte[] bytes)
        {
            int idx = 0;
            KeyExchangeRequest obj = new KeyExchangeRequest();
            obj.BuildFromBytes(bytes, ref idx);
            return obj;
        }

        public void BuildFromBytes(byte[] bytes, ref int idx)
        {
            this.PublicKey = BytesParser.GetBytes(bytes, ref idx);
        }

        public byte[] ToBytes()
        {
            BytesBuilder bb = new BytesBuilder();
            bb.Append(PublicKey);
            return bb.GetBytes();
        }
    }
}
