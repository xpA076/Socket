using FileManager.SocketLib.Enums;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FileManager.Models
{
    public sealed class Logger
    {
        private Logger()
        {

        }

        private static readonly Lazy<Logger> _client = new Lazy<Logger>(() => new Logger());

        public static Logger Client
        {
            get
            {
                return _client.Value;
            }
        }


        private static readonly Lazy<Logger> _server = new Lazy<Logger>(() => new Logger());

        public static Logger Server
        {
            get
            {
                return _server.Value;
            }
        }


        private ConcurrentQueue<string> LogQueue = new ConcurrentQueue<string>();


        public void InitClient()
        {
            Task.Run(() => { LogCycle("E:\\client.log"); });
        }

        public void InitServer()
        {
            Task.Run(() => { LogCycle("E:\\server.log"); });
        }


        public void Log(string logInfo, LogLevel logLevel = LogLevel.Info)
        {
            string time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string logLevel_str = "[" + logLevel.ToString().PadRight(5) + "]";
            string log_str = string.Format("{0} {1} {2}", time, logLevel_str, logInfo);
            LogQueue.Enqueue(log_str);
        }

        public void DebugLog(string log)
        {
            Log(log, LogLevel.Debug);
        }


        private void LogCycle(string path)
        {
            while (true)
            {
                if (LogQueue.Count > 0)
                {
                    using (FileStream stream = new FileStream(path, FileMode.Append))
                    using (StreamWriter writer = new StreamWriter(stream))
                    {
                        string log;
                        while (LogQueue.TryDequeue(out log))
                        {
                            writer.WriteLine(log);
                        }
                    }
                }
                Thread.Sleep(5000);
            }
        }


    }
}
