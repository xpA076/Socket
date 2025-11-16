using FileManager.Models.SocketLib.Enums;
using FileManager.Utils.Storage;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.Models.Log
{
    internal class LogService
    {
        private StoragePathMapper pathMapper = Program.Provider.GetService<StoragePathMapper>();

        private object loggerLock = new object();

        private string LogFileName
        {
            get
            {
                return "FileManager" + DateTime.Now.ToString("-yyyy-MM-dd") + ".log";
            }
        }

        private string ServerLogFileName
        {
            get
            {
                return "FileManagerServer" + DateTime.Now.ToString("-yyyy-MM-dd") + ".log";
            }
        }

        public void Log(string logInfo, LogLevel logLevel = LogLevel.Info)
        {
            lock (loggerLock)
            {
                string time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                string logLevel_str = "[" + logLevel.ToString().PadRight(5) + "]";
                /// log in file
                string filePath = Path.Combine(pathMapper.LogDirectory, LogFileName);
                using (FileStream stream = new FileStream(filePath, FileMode.Append))
                using (StreamWriter writer = new StreamWriter(stream))
                {
                    writer.WriteLine("{0} {1} {2}", time, logLevel_str, logInfo);
                }
            }
        }

        public void ServerLog(string logInfo, LogLevel logLevel)
        {
            ServerLog(logInfo, logLevel, DateTime.Now);
        }


        public void ServerLog(string logInfo, LogLevel logLevel, DateTime curr_time)
        {
            lock (loggerLock)
            {
                string time = curr_time.ToString("yyyy-MM-dd HH:mm:ss.fff");
                string logLevel_str = "[" + logLevel.ToString().PadRight(5) + "]";
                /// log in file
                string filePath = Path.Combine(pathMapper.LogDirectory, ServerLogFileName);
                using (FileStream stream = new FileStream(filePath, FileMode.Append))
                using (StreamWriter writer = new StreamWriter(stream))
                {
                    writer.WriteLine("{0} {1} {2}", time, logLevel_str, logInfo);
                }
            }
        }
    }
}
