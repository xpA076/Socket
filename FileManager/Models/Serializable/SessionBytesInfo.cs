using FileManager.SocketLib;
using FileManager.SocketLib.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FileManager.Models.Serializable
{
    /// <summary>
    /// 每个 SessionBytes 的内容, 客户端通信凭证
    /// 包含Server端索引(随机int, 唯一), 身份信息, 和验证字节
    /// Session 的索引和权限信息一经建立不再更新, 因此不必加锁
    /// </summary>
    public class SessionBytesInfo : ISocketSerializable
    {
        public const int BytesLength = 256;

        public int Index { get; private set; }

        public SocketIdentity Identity { get; private set; }

        public SessionBytesInfo()
        {

        }

        public SessionBytesInfo(int index, SocketIdentity identity)
        {
            Index = index;
            Identity = identity;
        }


        private byte[] VerificationBytes { get; set; } = new byte[BytesLength - 12]; /// 序列化时, 自身长度也占4字节

        private readonly ReaderWriterLockSlim VerificationBytesLock = new ReaderWriterLockSlim();

        public void UpdateVerificationBytes()
        {
            VerificationBytesLock.EnterWriteLock();
            Random rd = new Random();
            rd.NextBytes(VerificationBytes);
            VerificationBytesLock.ExitWriteLock();
        }


        public override bool Equals(object obj)
        {
            VerificationBytesLock.EnterReadLock();
            try
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
            }
            finally
            {
                VerificationBytesLock.ExitReadLock();
            }
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public static SessionBytesInfo FromBytes(byte[] bytes)
        {
            int idx = 0;
            SessionBytesInfo obj = new SessionBytesInfo();
            obj.BuildFromBytes(bytes, ref idx);
            return obj;
        }

        public byte[] ToBytes()
        {
            BytesBuilder bb = new BytesBuilder();
            bb.Append(this.Index);
            bb.Append((int)this.Identity);
            bb.Append(this.VerificationBytes);
            return bb.GetBytes();
        }

        public void BuildFromBytes(byte[] bytes, ref int idx)
        {
            this.Index = BytesParser.GetInt(bytes, ref idx);
            this.Identity = (SocketIdentity)BytesParser.GetInt(bytes, ref idx);
            this.VerificationBytes = BytesParser.GetBytes(bytes, ref idx);
        }
    }
}
