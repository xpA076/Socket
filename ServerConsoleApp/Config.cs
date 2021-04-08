using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ServerConsoleApp
{
    public static class Config
    {
        public static int ServerPort { get; set; } = 12138;

        public static int SocketSendTimeOut { get; set; } = 3000;

        public static int SocketReceiveTimeOut { get; set; } = 3000;

        public static int KeyLength { get; set; } = 16;
        public static List<string> AllowDirectoryList { get; set; } = new List<string>()
        {
            @"C:",
            @"D:",
            @"E:",
            @"F:",
            @"G:",
            @"H:",
            @"I:",
        };


        private static string ConfigPath
        {
            get
            {
                //string dir = System.Environment.CurrentDirectory;
                return System.Environment.CurrentDirectory + "\\ServerConsole.config";
            }
        }

        public static bool IsPathAllowed(string localPath)
        {
            return true;
        }

        public static void LoadConfig()
        {
            if (File.Exists(ConfigPath))
            {
                XDocument doc = XDocument.Load(ConfigPath);
                XElement root = doc.Root;
                ServerPort = int.Parse(root.Element("server").Element("serverPort").Value);
                SocketSendTimeOut = int.Parse(root.Element("connection").Element("socketSendTimeout").Value);
                SocketReceiveTimeOut = int.Parse(root.Element("connection").Element("socketReceiveTimeout").Value);
                AllowDirectoryList.Clear();
                foreach (XElement allowInfo in root.Element("allowList").Elements("directory"))
                {
                    AllowDirectoryList.Add(allowInfo.Element("content").Value);
                }
            }
            else
            {
                /// default config
                ServerPort = 12138;
                /// create xml donfig
                XElement root = new XElement("socketFileManagerConfig");
                XElement server = new XElement("server");
                server.SetElementValue("serverPort", ServerPort);
                root.Add(server);
                XElement connection = new XElement("connection");
                connection.SetElementValue("socketSendTimeout", SocketSendTimeOut.ToString());
                connection.SetElementValue("socketReceiveTimeout", SocketReceiveTimeOut.ToString());
                root.Add(connection);
                XElement allowList = new XElement("allowList");
                foreach(string path in AllowDirectoryList)
                {
                    XElement dir = new XElement("directory");
                    dir.SetElementValue("content", path);
                    allowList.Add(dir);
                }
                root.Add(allowList);
                root.Save(ConfigPath);
            }
        }
    }
}
