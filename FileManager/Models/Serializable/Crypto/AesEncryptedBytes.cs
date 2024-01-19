using FileManager.SocketLib;
using FileManager.Utils.Bytes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace FileManager.Models.Serializable.Crypto
{
    public class AesEncryptedBytes : ISocketSerializable
    {
        public byte[] IV { get; set; }

        public byte[] EncryptedBytes { get; set; }

        public static AesEncryptedBytes FromBytes(byte[] bytes)
        {
            int idx = 0;
            AesEncryptedBytes obj = new AesEncryptedBytes();
            obj.BuildFromBytes(bytes, ref idx);
            return obj;
        }

        public void BuildFromBytes(byte[] bytes, ref int idx)
        {
            this.IV = BytesParser.GetBytes(bytes, ref idx);
            this.EncryptedBytes = BytesParser.GetBytes(bytes, ref idx);
        }

        public byte[] ToBytes()
        {
            BytesBuilder bb = new BytesBuilder();
            bb.Append(this.IV);
            bb.Append(this.EncryptedBytes);
            return bb.GetBytes();
        }
    }
}
