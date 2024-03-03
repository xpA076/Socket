using FileManager.Models.SocketLib;
using FileManager.Utils.Bytes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace FileManager.Models.Serializable.Crypto
{
    public class SocketPrivateKey : IBytesSerializable
    {
        public SocketCertificate Certificate { get; set; } = new SocketCertificate();

        public byte[] PrivateKey { get; set; }

        public void BuildFromBytes(byte[] bytes, ref int idx)
        {
            this.Certificate = SocketCertificate.FromBytes(BytesParser.GetBytes(bytes, ref idx));
            this.PrivateKey = BytesParser.GetBytes(bytes, ref idx);
        }

        public byte[] ToBytes()
        {
            BytesBuilder bb = new BytesBuilder();
            bb.Append(Certificate.ToBytes());
            bb.Append(PrivateKey);
            return bb.GetBytes();
        }
    }
}
