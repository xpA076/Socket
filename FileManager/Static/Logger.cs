using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using SocketLib;
using SocketLib.Enums;

namespace FileManager.Static
{
    public static class Logger
    {
        private static object obj = new object();
        public static void ThreadPackage(int threadId, int package, string mode, string others)
        {
            lock (obj)
            {
                try
                {
                    using (FileStream stream = new FileStream("thread.log", FileMode.Append))
                    using (StreamWriter writer = new StreamWriter(stream))
                    {
                        writer.WriteLine("{0} : ThreadId {1} {3} package {2} - {4}", 
                            DateTime.Now.ToString("O"), threadId, package, mode, others);
                    }
                }
                catch {; }
            }
        }
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

    }
}
