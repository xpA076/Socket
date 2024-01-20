using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FileManager.Models.SocketLib.SocketServer.Services
{
    public class TimeoutCollector
    {
        private class TimeoutInfo
        {
            public int IdleSeconds { get; set; } = 0;
            public int TimeoutSeconds { get; set; } = 0;

            public bool IsTimeout()
            {
                return TimeoutSeconds > 0 && IdleSeconds > TimeoutSeconds;
            }
        }

        private static readonly Lazy<TimeoutCollector> _server_instance = new Lazy<TimeoutCollector>(() => new TimeoutCollector());

        public static TimeoutCollector ServerInstance => _server_instance.Value;

        public TimeoutCollector() { }


        public const int TickInterval = 30;

        private bool IsCollecting = true;

        private Dictionary<IDisposable, TimeoutInfo> Objects = new Dictionary<IDisposable, TimeoutInfo>();

        private readonly ReaderWriterLockSlim ObjectsLock = new ReaderWriterLockSlim();

        public void Register(IDisposable obj, int timeout)
        {
            ObjectsLock.EnterWriteLock();
            try
            {
                TimeoutInfo timeoutInfo = new TimeoutInfo()
                {
                    IdleSeconds = 0,
                    TimeoutSeconds = timeout
                };
                Objects.Add(obj, timeoutInfo);
            }
            finally
            {
                ObjectsLock.ExitWriteLock();
            }
        }

        public void UnRegister(IDisposable obj)
        {
            ObjectsLock.EnterWriteLock();
            try
            {
                if (Objects.ContainsKey(obj))
                {
                    Objects.Remove(obj);
                }
            }
            finally
            {
                ObjectsLock.ExitWriteLock();
            }

        }



        public void Refresh(IDisposable obj)
        {
            ObjectsLock.EnterReadLock();
            try
            {
                if (!Objects.ContainsKey(obj))
                {
                    throw new ObjectDisposedException("");
                }
                TimeoutInfo info = Objects[obj];
                lock (info)
                {
                    info.IdleSeconds = 0;
                }
            }
            finally
            {
                ObjectsLock.ExitReadLock();
            }
        }


        public void StartCollect()
        {
            IsCollecting = true;
            _ = Task.Run(() =>
            {
                Tick();
            });
        }


        public void StopCollect()
        {
            IsCollecting = false;
        }


        /// <summary>
        /// 在这里进行 Objects 中的对象计时, 以及过期对象的回收
        /// </summary>
        private void Tick()
        {
            while (IsCollecting)
            {
                Thread.Sleep(TickInterval * 1000);
                ObjectsLock.EnterWriteLock();
                try
                {
                    List<IDisposable> objs = new List<IDisposable>(Objects.Keys);
                    for (int i = 0; i < objs.Count; ++i)
                    {
                        IDisposable obj = objs[i];
                        lock (obj)
                        {
                            Objects[obj].IdleSeconds += TickInterval;
                            if (Objects[obj].IsTimeout())
                            {
                                Objects.Remove(obj);
                                obj.Dispose();
                            }
                        }
                    }
                }
                finally
                {
                    ObjectsLock.ExitWriteLock();
                }
            }
        }
    }
}
