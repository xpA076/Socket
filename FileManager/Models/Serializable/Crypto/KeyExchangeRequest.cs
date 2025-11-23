using FileManager.Models.EncryptLib;
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
        public required KeyExchangeMessage Message { get; set; }


        public static KeyExchangeRequest Build(ReadOnlySpan<byte> byteSpan)
        {
            return new KeyExchangeRequest { Message = KeyExchangeMessage.Build(byteSpan) };
        }

        public byte[] ToBytes()
        {
            BytesBuilder bb = new BytesBuilder();
            bb.Append(this.Message.EphemeralPublicKey);
            bb.Append(this.Message.IdentityPublicKey);
            bb.Append(this.Message.Signature);
            bb.Append(this.Message.Timestamp);
            bb.Append(this.Message.Salt);
            return bb.GetBytes();
        }
    }
}
