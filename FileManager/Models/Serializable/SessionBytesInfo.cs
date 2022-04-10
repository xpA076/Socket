using FileManager.SocketLib;
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

        public byte[] VerificationBytes { get; set; } = new byte[BytesLength - 12]; /// 序列化时, 自身长度也占4字节


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
            int idx = 0;
            this.Index = BytesParser.GetInt(bytes, ref idx);
            this.Identity = (SocketIdentity)BytesParser.GetInt(bytes, ref idx);
            this.VerificationBytes = BytesParser.GetBytes(bytes, ref idx);
        }

        public byte[] ToBytes()
        {
            BytesBuilder bb = new BytesBuilder();
            bb.Append(this.Index);
            bb.Append((int)this.Identity);
            bb.Append(this.VerificationBytes);
            return bb.GetBytes();
        }
    }
}
