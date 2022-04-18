using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.Exceptions.Server
{
    public class ServerFileException : ServerInternalException
    {
        public ServerFileException() : base()
        {

        }

        public ServerFileException(string msg) : base(msg)
        {

        }

    }
}
