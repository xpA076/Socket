using FileManager.Events;
using FileManager.SocketLib.SocketServer.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.SocketLib.SocketServer.Models
{
    public class FileResource : IDisposable
    {
        public FileAccess FileAccess { get; set; }

        public string Path { get; set; }

        private FileStream FileStream { get; set; } = null;

        private readonly object FileStreamLock = new object();

        public FileResource(string path, FileAccess access)
        {
            this.Path = path;
            this.FileAccess = access;
            if (access == FileAccess.Read)
            {
                FileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
            }
            else
            {
                FileStream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write);
            }
        }


        public byte[] ReadSpan(long begin, long end)
        {

            TimeoutCollector.ServerInstance.Refresh(this);

            throw new NotImplementedException();
        }


        public void WriteSpan(long begin, long end, byte[] bytes)
        {
            TimeoutCollector.ServerInstance.Refresh(this);
        }


        #region Dispose
        public event DisposeEventHandler ManagedDispose;
        
        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 释放托管状态(托管对象)
                    ManagedDispose(this, EventArgs.Empty);

                }
                // TODO: 释放未托管的资源(未托管的对象)并重写终结器
                // TODO: 将大型字段设置为 null
                FileStream.Close();
                disposedValue = true;
            }
        }

        // // TODO: 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        // ~FileResource()
        // {
        //     // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
