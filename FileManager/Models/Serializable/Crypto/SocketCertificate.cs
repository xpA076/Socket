using FileManager.Models.SocketLib;
using FileManager.Utils.Bytes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.Models.Serializable.Crypto
{
    public class SocketCertificate : ISocketSerializable
    {
        public int Version { get; set; } = 1;

        public DateTime StartTime { get; set; }

        public DateTime ExpireTime { get; set; }

        public byte[] PublicKey { get; set; }

        public byte[] Signature { get; set; }

        public void BuildFromBytes(byte[] bytes, ref int idx)
        {
            this.Version = BytesParser.GetInt(bytes, ref idx);
            this.StartTime = BytesParser.GetDateTime(bytes, ref idx);
            this.ExpireTime = BytesParser.GetDateTime(bytes, ref idx);
            this.PublicKey = BytesParser.GetBytes(bytes, ref idx);
            this.Signature = BytesParser.GetBytes(bytes, ref idx);
        }

        public byte[] ToBytes()
        {
            BytesBuilder bb = new BytesBuilder();
            bb.Append(Version);
            bb.Append(StartTime);
            bb.Append(ExpireTime);
            bb.Append(PublicKey);
            bb.Append(Signature);
            return bb.GetBytes();
        }

        public static SocketCertificate FromBytes(byte[] bytes, int idx = 0)
        {
            SocketCertificate obj = new SocketCertificate();
            obj.BuildFromBytes(bytes, ref idx);
            return obj;
        }

        public byte[] InfoToBytes()
        {
            BytesBuilder bb = new BytesBuilder();
            bb.Append(Version);
            bb.Append(StartTime);
            bb.Append(ExpireTime);
            bb.Append(PublicKey);
            return bb.GetBytes();
        }


        public bool Equals(SocketCertificate obj)
        {
            if (obj == null) return false;
            if (this.Version != obj.Version) return false;
            if (this.StartTime != obj.StartTime) return false;
            if (this.ExpireTime != obj.ExpireTime) return false;
            for (int i = 0; i < this.PublicKey.Length; i++)
            {
                if (this.PublicKey[i] != obj.PublicKey[i]) return false;
            }
            for (int i = 0; i < this.Signature.Length; i++)
            {
                if (this.Signature[i] != obj.Signature[i]) return false;
            }
            return true;
        }
    }
}
