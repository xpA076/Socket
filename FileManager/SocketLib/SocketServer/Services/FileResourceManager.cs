using FileManager.Exceptions;
using FileManager.Exceptions.Server;
using FileManager.SocketLib.SocketServer.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FileManager.SocketLib.SocketServer.Services
{
    public class FileResourceManager
    {
        private readonly Dictionary<string, FileResource> FileResources = new Dictionary<string, FileResource>();

        private readonly ReaderWriterLockSlim FileResourcesLock = new ReaderWriterLockSlim();


        public FileResource GetResource(string path, FileAccess access)
        {
            FileResourcesLock.EnterUpgradeableReadLock();
            try
            {
                if (FileResources.ContainsKey(path))
                {
                    FileResource resource = FileResources[path];
                    if (access == resource.FileAccess)
                    {
                        if (resource.FileAccess == FileAccess.Read)
                        {
                            return resource;
                        }
                        else
                        {
                            throw new ServerInternalException("FileResourceManager.GetResource() : FileResource writter is occupied.");
                        }
                    }
                    else
                    {
                        throw new ServerInternalException("FileResourceManager.GetResource() : FileMode not match.");
                    }
                }
                else
                {
                    FileResourcesLock.EnterWriteLock();
                    try
                    {
                        FileResource resource = CreateResource(path, access);
                        FileResources.Add(path, resource);
                        return resource;
                    }
                    finally
                    {
                        FileResourcesLock.ExitWriteLock();
                    }
                }
            }
            finally
            {
                FileResourcesLock.ExitUpgradeableReadLock();
            }
        }


        private FileResource CreateResource(string path, FileAccess access)
        {
            FileResource resource = new FileResource(path, access);
            TimeoutCollector.ServerInstance.Register(resource, 300);
            resource.ManagedDispose += OnResourceDispose;
            return resource;
        }


        private void OnResourceDispose(object sender, EventArgs e)
        {
            FileResourcesLock.EnterWriteLock();
            try
            {
                FileResource resource = (FileResource)sender;
                FileResources.Remove(resource.Path);
            }
            finally
            {
                FileResourcesLock.ExitWriteLock();
            }
        }


    }
}
