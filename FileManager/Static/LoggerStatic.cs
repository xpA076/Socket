using FileManager.Models.SocketLib.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace FileManager.Static
{
    public static class LoggerStatic
    {



        private static object loggerLock = new object();

        public static void Log(string logInfo, LogLevel logLevel = LogLevel.Info)
        {
            lock (loggerLock)
            {
                string time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                string logLevel_str = "[" + logLevel.ToString().PadRight(5) + "]";
                /// log in file
                string fileName = Config.LogDir + "FileManager" + DateTime.Now.ToString("-yyyy-MM-dd") + ".log";
                using (FileStream stream = new FileStream(fileName, FileMode.Append))
                using (StreamWriter writer = new StreamWriter(stream))
                {
                    writer.WriteLine("{0} {1} {2}", time, logLevel_str, logInfo);
                }
            }
        }

        public static void ServerLog(string logInfo, LogLevel logLevel)
        {
            ServerLog(logInfo, logLevel, DateTime.Now);
        }


        public static void ServerLog(string logInfo, LogLevel logLevel, DateTime curr_time)
        {
            lock (loggerLock)
            {
                string time = curr_time.ToString("yyyy-MM-dd HH:mm:ss.fff");
                string logLevel_str = "[" + logLevel.ToString().PadRight(5) + "]";
                /// log in file
                string fileName = Config.LogDir + "FileManagerServer" + curr_time.ToString("-yyyy-MM-dd") + ".log";
                using (FileStream stream = new FileStream(fileName, FileMode.Append))
                using (StreamWriter writer = new StreamWriter(stream))
                {
                    writer.WriteLine("{0} {1} {2}", time, logLevel_str, logInfo);
                }
            }
        }

    }
}
