using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.Exceptions
{
    public class SocketSessionException : Exception
    {
        private string _msg = "Default error message";

        public override string Message => _msg;

        public SocketSessionException()
        {
            
        }


        public SocketSessionException(string msg)
        {
            _msg = msg;
        }

    }
}
