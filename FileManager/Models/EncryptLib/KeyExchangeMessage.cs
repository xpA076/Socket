using FileManager.Utils.Bytes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.Models.EncryptLib
{
    public class KeyExchangeMessage
    {
        public required byte[] EphemeralPublicKey { get; set; }
        public required byte[] IdentityPublicKey { get; set; }
        public required byte[] Signature { get; set; }
        public required byte[] Timestamp { get; set; }
        public required byte[] Salt { get; set; }

        public static KeyExchangeMessage Build(ReadOnlySpan<byte> byteSpan)
        {
            byte[] bytes = byteSpan.ToArray();
            int idx = 0;
            var EphemeralPublicKey = BytesParser.GetBytes(bytes, ref idx);
            var IdentityPublicKey = BytesParser.GetBytes(bytes, ref idx);
            var Signature = BytesParser.GetBytes(bytes, ref idx);
            var Timestamp = BytesParser.GetBytes(bytes, ref idx);
            var Salt = BytesParser.GetBytes(bytes, ref idx);
            return new KeyExchangeMessage()
            {
                EphemeralPublicKey = EphemeralPublicKey,
                IdentityPublicKey = IdentityPublicKey,
                Signature = Signature,
                Timestamp = Timestamp,
                Salt = Salt
            };
        }

        public static KeyExchangeMessage Empty
        {
            get
            {
                return new KeyExchangeMessage
                {
                    EphemeralPublicKey = new byte[0],
                    IdentityPublicKey = new byte[0],
                    Signature = new byte[0],
                    Timestamp = new byte[0],
                    Salt = new byte[0]
                };
            }
        }

    }
}
