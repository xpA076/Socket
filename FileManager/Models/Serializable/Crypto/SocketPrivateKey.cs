using FileManager.SocketLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.Models.Serializable.Crypto
{
    public class SocketPrivateKey : IBytesSerializable
    {
        public SocketCertificate Certificate { get; set; }

        public byte[] PrivateKey { get; set; }

        public void BuildFromBytes(byte[] bytes, ref int idx)
        {
            throw new NotImplementedException();
        }

        public byte[] ToBytes()
        {
            throw new NotImplementedException();
        }
    }
}
