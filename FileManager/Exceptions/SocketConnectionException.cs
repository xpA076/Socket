using FileManager.Models.SocketLib.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.Exceptions
{
    public class SocketConnectionException : Exception
    {
        public override string Message { get; } = "";

        public SocketStatus Status { get; } = SocketStatus.Connected;

        public SocketConnectionException()
        {

        }

        public SocketConnectionException(SocketStatus status, string message = "")
        {
            this.Status = status;
            this.Message = message;
        }

        public SocketConnectionException(string s)
        {
            this.Message = s;
        }
    }
}
