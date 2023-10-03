using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.Exceptions
{
    public class SocketConnectionException : Exception
    {
        public override string Message { get; }

        public SocketConnectionException()
        {

        }

        public SocketConnectionException(string s)
        {
            this.Message = s;
        }
    }
}
