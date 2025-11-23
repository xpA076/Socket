using FileManager.Utils.Bytes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.Models.Serializable.Crypto
{
    public class AesGcmEncryptedBytes
    {
        public byte[] EncryptedBytes = [];

        public byte[] AssociatedData = Encoding.UTF8.GetBytes("AES-GCM-PROTOCOL-251118");

        public static AesGcmEncryptedBytes FromBytes(byte[] bytes)
        {
            int idx = 0;
            AesGcmEncryptedBytes obj = new AesGcmEncryptedBytes();
            obj.BuildFromBytes(bytes, ref idx);
            return obj;
        }

        public void BuildFromBytes(byte[] bytes, ref int idx)
        {
            this.EncryptedBytes = BytesParser.GetBytes(bytes, ref idx);
            this.AssociatedData = BytesParser.GetBytes(bytes, ref idx);
        }

        public byte[] ToBytes()
        {
            BytesBuilder bb = new BytesBuilder();
            bb.Append(this.EncryptedBytes);
            bb.Append(this.AssociatedData);
            return bb.GetBytes();
        }
    }
}
