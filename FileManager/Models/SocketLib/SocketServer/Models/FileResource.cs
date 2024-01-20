using FileManager.Events;
using FileManager.Exceptions;
using FileManager.Exceptions.Server;
using FileManager.Models.SocketLib.SocketServer.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.Models.SocketLib.SocketServer.Models
{
    /// <summary>
    /// FileStream 的封装类, 实现文件读写操作, 并保证线程安全
    /// </summary>
    public class FileResource : IDisposable
    {
        public FileAccess FileAccess { get; set; }

        public string ServerPath { get; set; }

        public int WriterSessionIndex { get; set; }

        private FileStream FileStream { get; set; } = null;

        private readonly object FileStreamLock = new object();

        public FileResource(string path, FileAccess access)
        {
            this.ServerPath = path;
            this.FileAccess = access;
            if (access == FileAccess.Read)
            {
                FileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
            }
            else
            {
                /// 连续创建目录直至目标文件夹
                string server_dir = path.Substring(0, path.LastIndexOf('\\') + 1);
                List<int> slashes = new List<int>();
                for (int idx = 0; idx < server_dir.Length; ++idx)
                {
                    if (server_dir[idx] == '\\' || server_dir[idx] == '/')
                    {
                        slashes.Add(idx);
                    }
                }
                foreach (int idx in slashes)
                {
                    string dir = server_dir.Substring(0, idx + 1);
                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }
                }
                /// 打开对应文件
                FileStream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write);
            }
        }


        public byte[] ReadSpan(long begin, long length)
        {
            TimeoutCollector.ServerInstance.Refresh(this);
            if (FileAccess != FileAccess.Read)
            {
                throw new ServerFileException("Invalid FileAccess type");
            }
            if (length <= 0)
            {
                throw new ServerFileException("Invalid input argument");
            }
            byte[] bytes = new byte[length];
            try
            {
                lock (FileStreamLock)
                {
                    FileStream.Seek(begin, SeekOrigin.Begin);
                    FileStream.Read(bytes, 0, bytes.Length);
                }
            }
            catch (Exception ex)
            {
                throw new ServerFileException("ReadSpan() exception : " + ex.Message);
            }
            return bytes;
        }


        public void WriteSpan(long offset, byte[] bytes, int length)
        {
            TimeoutCollector.ServerInstance.Refresh(this);
            if (FileAccess != FileAccess.Write)
            {
                throw new ServerFileException("Invalid FileAccess type");
            }
            if (length <= 0)
            {
                throw new ServerFileException("Invalid input argument");
            }
            try
            {
                lock (FileStreamLock)
                {
                    FileStream.Seek(offset, SeekOrigin.Begin);
                    FileStream.Write(bytes, 0, length);
                }
            }
            catch (Exception ex)
            {
                throw new ServerFileException("WriteSpan() exception : " + ex.Message);
            }
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
                lock (FileStreamLock)
                {
                    FileStream.Close();
                }
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
