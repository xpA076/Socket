using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FileManager.SocketLib.Enums;


namespace FileManager.Events
{
    public delegate void SocketLogEventHandler(object sender, SocketLogEventArgs e);

    public class SocketLogEventArgs : EventArgs
    {
        public readonly string log;
        public readonly LogLevel logLevel;
        public readonly DateTime time = DateTime.Now;

        public SocketLogEventArgs(string log, LogLevel logLevel)
        {
            this.log = log;
            this.logLevel = logLevel;
        }

    }
}
