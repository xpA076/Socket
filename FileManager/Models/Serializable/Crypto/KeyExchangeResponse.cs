using FileManager.Models.EncryptLib;
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
        public enum Status: int
        {
            Success,
            UnauthedClient,
            VerifyMessageFailed
        }

        public required Status ResponseStatus {  get; set; }

        public required KeyExchangeMessage Message { get; set; }

        public static KeyExchangeResponse Build(ReadOnlySpan<byte> byteSpan)
        {
            int idx = 0;
            Status status = (Status)BytesParser.GetInt(byteSpan.Slice(0, 4).ToArray(), ref idx);
            KeyExchangeMessage message = KeyExchangeMessage.Build(byteSpan.Slice(4));
            return new KeyExchangeResponse 
            {
                ResponseStatus = status, 
                Message = message
            };
        }

        public byte[] ToBytes()
        {
            BytesBuilder bb = new BytesBuilder();
            bb.Append((int)ResponseStatus);
            bb.Append(this.Message.EphemeralPublicKey);
            bb.Append(this.Message.IdentityPublicKey);
            bb.Append(this.Message.Signature);
            bb.Append(this.Message.Timestamp);
            bb.Append(this.Message.Salt);
            return bb.GetBytes();
        }
    }
}
