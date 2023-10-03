using FileManager.Models.Serializable;
using FileManager.SocketLib.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FileManager.SocketLib.SocketServer
{
    /// <summary>
    /// Server 端的 Session 实例
    /// 重点实现 SetSession() 和 GetSession() 方法
    /// </summary>
    public class SocketSession
    {
        private readonly Dictionary<string, object> Objects = new Dictionary<string, object>();

        private readonly ReaderWriterLockSlim ObjectsLock = new ReaderWriterLockSlim();

        /// <summary>
        /// 这个为确认 session 用的 bytes
        /// client 端建立新连接 / 设置session内容等情况下会发送
        /// 头部包含 Index, Identity等信息
        /// </summary>
        public SessionBytesInfo BytesInfo;

        public SocketSession(SessionBytesInfo bytesInfo)
        {
            BytesInfo = bytesInfo;
        }

        /// 权限验证部分, 因为每个Session的权限信息不会被改变, 因此不用加锁读取
        #region Authentication

        public bool AllowQuery()
        {
            return (BytesInfo.Identity & SocketIdentity.Query) > 0;
        }

        public bool AllowReadFile()
        {
            return (BytesInfo.Identity & SocketIdentity.ReadFile) > 0;
        }

        public bool AllowWriteFile()
        {
            return (BytesInfo.Identity & SocketIdentity.WriteFile) > 0;
        }

        #endregion

        public void SetSession(string name, object obj)
        {
            try
            {
                ObjectsLock.EnterWriteLock();
                Objects.Add(name, obj);
            }
            finally
            {
                ObjectsLock.ExitWriteLock();
            }
        }

        public object GetSession(string name)
        {
            try
            {
                ObjectsLock.EnterReadLock();
                return Objects[name];
            }
            finally
            {
                ObjectsLock.ExitReadLock();
            }
        }

        public bool RemoveSession(string name)
        {
            try
            {
                ObjectsLock.EnterWriteLock();
                if (Objects.ContainsKey(name))
                {
                    Objects.Remove(name);
                    return true;
                }
                else
                {
                    return false;
                }
            }
            finally
            {
                ObjectsLock.ExitWriteLock();
            }
        }


    }
}
