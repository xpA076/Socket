using FileManager.Models.SocketLib;
using FileManager.Utils.Bytes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace FileManager.Models.Serializable.Crypto
{
    public class KeyExchangeResponse : ISocketSerializable
    {
        public bool RequestCertificateValid { get; set; } = true;

        public SocketCertificate Certificate { get; set; } = new SocketCertificate();

        public byte[] EcdhPublicKey { get; set; } = new byte[0];

        public byte[] Signature { get; set; } = new byte[0];

        public static KeyExchangeResponse FromBytes(byte[] bytes, int idx = 0)
        {
            KeyExchangeResponse obj = new KeyExchangeResponse();
            obj.BuildFromBytes(bytes, ref idx);
            return obj;
        }

        public void BuildFromBytes(byte[] bytes, ref int idx)
        {
            this.RequestCertificateValid = BytesParser.GetBool(bytes, ref idx);
            this.Certificate = SocketCertificate.FromBytes(BytesParser.GetBytes(bytes, ref idx));
            this.EcdhPublicKey = BytesParser.GetBytes(bytes, ref idx);
            this.Signature = BytesParser.GetBytes(bytes, ref idx);
        }

        public byte[] ToBytes()
        {
            BytesBuilder bb = new BytesBuilder();
            bb.Append(RequestCertificateValid);
            bb.Append(Certificate.ToBytes());
            bb.Append(EcdhPublicKey);
            bb.Append(Signature);
            return bb.GetBytes();
        }
    }
}
