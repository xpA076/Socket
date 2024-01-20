using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.Events
{
    public class SocketAsyncExceptionEventArgs : EventArgs
    {
        public string ExceptionMessage => ThrowedException.Message;

        public Exception ThrowedException { get; set; }

        public SocketAsyncExceptionEventArgs(Exception ex)
        {
            ThrowedException = ex;
        }
    }
}
