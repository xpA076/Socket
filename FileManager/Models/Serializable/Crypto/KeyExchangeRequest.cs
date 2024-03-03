using FileManager.Models.SocketLib;
using FileManager.Utils.Bytes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.Models.Serializable.Crypto
{
    public class KeyExchangeRequest : ISocketSerializable
    {
        public SocketCertificate Certificate { get; set; } = new SocketCertificate();

        public byte[] EcdhPublicKey { get; set; }

        public byte[] Signature { get; set; }

        public static KeyExchangeRequest FromBytes(byte[] bytes, int idx = 0)
        {
            KeyExchangeRequest obj = new KeyExchangeRequest();
            obj.BuildFromBytes(bytes, ref idx);
            return obj;
        }

        public void BuildFromBytes(byte[] bytes, ref int idx)
        {
            this.Certificate = SocketCertificate.FromBytes(BytesParser.GetBytes(bytes, ref idx));
            this.EcdhPublicKey = BytesParser.GetBytes(bytes, ref idx);
            this.Signature = BytesParser.GetBytes(bytes, ref idx);
        }

        public byte[] ToBytes()
        {
            BytesBuilder bb = new BytesBuilder();
            bb.Append(Certificate.ToBytes());
            bb.Append(EcdhPublicKey);
            bb.Append(Signature);
            return bb.GetBytes();
        }
    }
}
