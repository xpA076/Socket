using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.Exceptions.Server
{
    /// <summary>
    /// Server 内部异常, 是一部分异常类的基类
    /// </summary>
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
