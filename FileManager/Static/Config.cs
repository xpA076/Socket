using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Linq;
using FileManager.Models;


namespace FileManager.Static
{
    public static class Config
    {
        public static bool UseLegacyFileInfo { get; set; } = true;


        #region UI
        /// <summary>
        /// 窗口关闭操作
        /// </summary>
        public static bool ClickCloseToMinimize { get; set; } = true;

        /// <summary>
        /// 刷新界面所需传输最小字节数
        /// </summary>
        public static long UpdateLengthThreshold { get; set; } = 128 * 1024;
        /// <summary>
        /// 刷新界面最短时间间隔 (ms)
        /// </summary>
        public static int UpdateTimeThreshold { get; set; } = 500;

        #endregion



        #region HeartBeatConnection 相关参数
        public static int ConnectionMonitorRecordCount { get; set; } = 10;
        public static int ConnectionMonitorRecordInterval { get; set; } = 3000;

        #endregion

        public static int DefaultServerPort { get; set; } = 12138;

        public static int DefaultProxyPort { get; set; } = 12139;

        public static int BuildConnectionTimeout { get; set; } = 2000;

        public static int SocketSendTimeout { get; set; } = 5000;

        public static int SocketReceiveTimeout { get; set; } = 5000;

        /// <summary>
        /// 启动多线程传输文件大小阈值
        /// </summary>
        public static long SmallFileThreshold { get; set; } = 4 * 1024 * 1024;

        public static int ThreadLimit { get; set; } = 16;

        /// <summary>
        /// Transfer 过程中保存 Record 间隔
        /// </summary>
        public static int SaveRecordInterval { get; set; } = 5000;


        

        public static byte[] KeyBytes { get; set; } = new byte[256];

        

        public static ObservableCollection<ConnectionRecord> Histories = new ObservableCollection<ConnectionRecord>();

        public static ObservableCollection<ConnectionRecord> Stars = new ObservableCollection<ConnectionRecord>();

        private static string _log_path = "";

        public static string ConfigDir
        {
            get
            {
                return Environment.GetEnvironmentVariable("APPDATA") + "\\FileManager\\";
            }
        }

        public static string LogDir
        {
            get
            {
                return ConfigDir;
            }
        }

        public static string ConfigPath
        {
            get
            {
                // return Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName) + "\\SocketFileManager.config";
                return ConfigDir + "FileManager.config";
            }
        }


        public static string ServerConfigPath
        {
            get
            {
                return ConfigDir + "FileManagerServer.config";
            }
        }

        public static void InsertHistory(ConnectionRecord connectionRecord)
        {
            for (int i = 0; i < Histories.Count;)
            {
                if (Histories[i].Info == connectionRecord.Info)
                {
                    connectionRecord.CopyFrom(Histories[i]);
                    Histories.RemoveAt(i);
                    continue;
                }
                else
                {
                    i++;
                }
            }
            Histories.Insert(0, connectionRecord);
            SaveConfig();
        }

        public static void Star(ConnectionRecord connectionRecord)
        {
            connectionRecord.Star();
            Stars.Insert(0, connectionRecord.Copy());
            SaveConfig();
        }

        public static void UnStar(ConnectionRecord connectionRecord)
        {
            for (int i = 0; i < Stars.Count;)
            {
                if (Stars[i].Info == connectionRecord.Info)
                {
                    Stars.RemoveAt(i);
                    continue;
                }
                else
                {
                    i++;
                }
            }
            foreach(ConnectionRecord c in Histories)
            {
                if (c.Info == connectionRecord.Info)
                {
                    c.Unstar();
                }
            }
            SaveConfig();
        }


        public static void LoadConfig()
        {
            _log_path = System.IO.Path.GetDirectoryName(
                System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName
                );

            if (!File.Exists(ConfigPath))
            {
                string appdata = Environment.GetEnvironmentVariable("APPDATA");
                if (!Directory.Exists(appdata + "\\FileManager"))
                {
                    Directory.CreateDirectory(appdata + "\\FileManager");
                }
            }
            try
            {
                XDocument doc = XDocument.Load(ConfigPath);
                XElement root = doc.Root;

                /// Records
                XElement record = root.Element("record");
                Histories.Clear();
                Stars.Clear();
                foreach (XElement c in record.Element("history").Elements("item"))
                {
                    ConnectionRecord connectionRecord = new ConnectionRecord
                    {
                        Info = c.Element("ip").Value,
                        IsStarred = c.Element("star").Value != "0"
                    };
                    Histories.Add(connectionRecord);
                    if (connectionRecord.IsStarred)
                    {
                        Stars.Add(connectionRecord.Copy());
                    }
                }

                /// Settings
                XElement settings = root.Element("settings");

                ClickCloseToMinimize = bool.Parse(settings.Element("ClickCloseToMinimize").Value);
                UpdateLengthThreshold = long.Parse(settings.Element("UpdateLengthThreshold").Value);
                UpdateTimeThreshold = int.Parse(settings.Element("UpdateTimeThreshold").Value);
                SaveRecordInterval = int.Parse(settings.Element("SaveRecordInterval").Value);
                ConnectionMonitorRecordCount = int.Parse(settings.Element("ConnectionMonitorRecordCount").Value);
                ConnectionMonitorRecordInterval = int.Parse(settings.Element("ConnectionMonitorRecordInterval").Value);
                DefaultServerPort = int.Parse(settings.Element("DefaultPort").Value);
                ThreadLimit = int.Parse(settings.Element("ThreadLimit").Value);
                SmallFileThreshold = long.Parse(settings.Element("SmallFileLimit").Value);
                SocketSendTimeout = int.Parse(settings.Element("SocketSendTimeout").Value);
                SocketReceiveTimeout = int.Parse(settings.Element("SocketReceiveTimeout").Value);
            }
            catch (Exception)
            {
                SaveConfig();
            }
        }

        public static void SaveConfig()
        {
            /// Create xml config
            XElement root = new XElement("FileManagerConfig");

            /// Records
            XElement record = new XElement("record");
            /// History list
            XElement history_list = new XElement("history");
            foreach (ConnectionRecord c in Histories)
            {
                XElement item = new XElement("item");
                item.SetElementValue("ip", c.Info);
                item.SetElementValue("star", c.IsStarred ? "1" : "0");
                history_list.Add(item);
            }
            record.Add(history_list);
            root.Add(record);

            /// Settings
            XElement settings = new XElement("settings");
            settings.SetElementValue("ClickCloseToMinimize", ClickCloseToMinimize.ToString());
            settings.SetElementValue("UpdateLengthThreshold", UpdateLengthThreshold.ToString());
            settings.SetElementValue("UpdateTimeThreshold", UpdateTimeThreshold.ToString());
            settings.SetElementValue("SaveRecordInterval", SaveRecordInterval.ToString());
            settings.SetElementValue("ConnectionMonitorRecordCount", ConnectionMonitorRecordCount.ToString());
            settings.SetElementValue("ConnectionMonitorRecordInterval", ConnectionMonitorRecordInterval.ToString());
            settings.SetElementValue("DefaultPort", DefaultServerPort.ToString());
            settings.SetElementValue("ThreadLimit", ThreadLimit.ToString());
            settings.SetElementValue("SmallFileLimit", SmallFileThreshold.ToString());
            settings.SetElementValue("SocketSendTimeout", SocketSendTimeout.ToString());
            settings.SetElementValue("SocketReceiveTimeout", SocketReceiveTimeout.ToString());

            root.Add(settings);
            root.Save(ConfigPath);
        }
    }
}
