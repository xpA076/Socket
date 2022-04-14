using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.Exceptions
{
    public class ServerInternalException : Exception
    {
        private string _msg = "Default error message";

        public override string Message => _msg;

        public ServerInternalException()
        {

        }


        public ServerInternalException(string msg)
        {
            _msg = msg;
        }




    }
}
