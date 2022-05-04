using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.Models.TransferLib.Services
{
    public class TransferDiskManager
    {
        public const long BlockSize = 4096;
        
        public string Path { get; private set; }

        public FileAccess FileAccess { get; private set; }

        private readonly object FileLock = new object();

        private bool IsFileStreamClosed = true;

        private FileStream FileStream = null;

        public void SetPath(string path, FileAccess access)
        {
            lock (FileLock)
            {
                if (!IsFileStreamClosed)
                {
                    FileStream.Close();
                }
                Path = path;
                FileAccess = access;
                if (access == FileAccess.Read)
                {
                    FileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
                }
                else
                {
                    FileStream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write);
                }
                IsFileStreamClosed = false;
            }
        }


        public void Finish()
        {
            lock (FileLock)
            {
                FileStream.Close();
                IsFileStreamClosed = true;
            }
        }


        public void WriteBytes(long offset, byte[] bytes)
        {
            WriteBytes(offset, bytes, bytes.Length);
        }
        

        public void WriteBytes(long offset, byte[] bytes, int length)
        {
            lock (FileLock)
            {
                FileStream.Seek(offset, SeekOrigin.Begin);
                FileStream.Write(bytes, 0, length);
            }
        }


        public byte[] ReadBytes(long offset, int length)
        {
            lock (FileLock)
            {
                byte[] bytes = new byte[length];
                FileStream.Seek(offset, SeekOrigin.Begin);
                FileStream.Read(bytes, 0, length);
                return bytes;
            }
        }


    }



}
