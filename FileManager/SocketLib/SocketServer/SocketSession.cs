using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FileManager.SocketLib.SocketServer
{
    /// <summary>
    /// Server 端的 Session 实例, 同一个 Client 端的所有 TCP 连接对应同一个Session
    /// 重点实现 SetSession() 和 GetSession() 方法
    /// </summary>
    public class SocketSession
    {
        private readonly Dictionary<string, object> Objects = new Dictionary<string, object>();

        private readonly ReaderWriterLockSlim ObjectsLock = new ReaderWriterLockSlim();


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
