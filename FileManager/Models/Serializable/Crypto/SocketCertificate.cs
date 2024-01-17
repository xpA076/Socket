using FileManager.SocketLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.Models.Serializable.Crypto
{
    public class HbCertificate : ISocketSerializable
    {
        public int Version { get; set; } = 1;

        public DateTime StartTime { get; set; }

        public DateTime ExpireTime { get; set; }

        public byte[] PublicKey { get; set; }

        public void BuildFromBytes(byte[] bytes, ref int idx)
        {
            this.Version = BytesParser.GetInt(bytes, ref idx);
            this.StartTime = BytesParser.GetDateTime(bytes, ref idx);
            this.ExpireTime = BytesParser.GetDateTime(bytes, ref idx);
            this.PublicKey = BytesParser.GetBytes(bytes, ref idx);
        }

        public byte[] ToBytes()
        {
            BytesBuilder bb = new BytesBuilder();
            bb.Append(Version);
            bb.Append(StartTime);
            bb.Append(ExpireTime);
            bb.Append(PublicKey);
            return bb.GetBytes();
        }
    }
}
