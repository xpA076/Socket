using FileManager.SocketLib.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.Models.Serializable
{
    public class SessionBytesInfo : ISocketSerializable
    {
        public const int BytesLength = 256;

        public int Index { get; set; }

        public SocketIdentity Identity { get; set; }

        public byte[] VerificationBytes { get; set; } = new byte[BytesLength - 8];


        public override bool Equals(object obj)
        {
            SessionBytesInfo sbi = obj as SessionBytesInfo;
            if (Index == sbi.Index && Identity == sbi.Identity && 
                VerificationBytes.Length == sbi.VerificationBytes.Length)
            {
                for (int i = 0; i < VerificationBytes.Length; ++i)
                {
                    if (VerificationBytes[i] != sbi.VerificationBytes[i])
                    {
                        return false;
                    }
                }
                return true;
            }
            else
            {
                return false;
            }
            //return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public static SessionBytesInfo FromBytes(byte[] bytes)
        {
            SessionBytesInfo obj = new SessionBytesInfo();
            obj.BuildFromBytes(bytes);
            return obj;
        }


        public void BuildFromBytes(byte[] bytes)
        {
            throw new NotImplementedException();
        }

        public byte[] ToBytes()
        {
            throw new NotImplementedException();
        }
    }
}
