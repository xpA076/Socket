using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Linq;

namespace SocketFileManager
{
    public static class Config
    {
        public static int ServerPort { get; private set; }

        public static int ThreadLimit { get; set; } = 10;

        public static long SmallFileLimit { get; set; } = 4 * 1024 * 1024;

        public static int SocketSendTimeOut { get; set; } = 3000;

        public static int SocketReceiveTimeOut { get; set; } = 3000;

        public static string LastConnect {
            get {
                try
                {
                    XDocument doc = XDocument.Load(configPath);
                    return doc.Root.Element("connection").Element("lastConnect").Value;
                }
                catch (Exception) { return ""; }
            }
            set{
                XDocument doc = XDocument.Load(configPath);
                doc.Root.Element("connection").SetElementValue("lastConnect", value);
                doc.Save(configPath);
            }
        }

        private static string configPath
        {
            get
            {
                return Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName) + "\\SocketFileManager.config";
            }
        }

        public static void LoadConfig()
        {
            if (!File.Exists(configPath))
            {
                // default config
                ServerPort = 12138;
                // create xml config
                XDocument doc = new XDocument();
                XElement root = new XElement("socketFileManagerConfig");
                XElement server = new XElement("server");
                server.SetElementValue("serverPort", ServerPort);
                root.Add(server);
                XElement connection = new XElement("connection");
                connection.SetElementValue("lastConnect", "");
                connection.SetElementValue("threadLimit", ThreadLimit.ToString());
                connection.SetElementValue("smallFileLimit", SmallFileLimit.ToString());
                connection.SetElementValue("socketSendTimeout", SocketSendTimeOut.ToString());
                connection.SetElementValue("socketReceiveTimeout", SocketReceiveTimeOut.ToString());
                root.Add(connection);
                root.Save(configPath);
            }
            else
            {
                XDocument doc = XDocument.Load(configPath);
                XElement root = doc.Root;
                ServerPort = int.Parse(root.Element("server").Element("serverPort").Value);
                ThreadLimit = int.Parse(root.Element("connection").Element("threadLimit").Value);
                SmallFileLimit = long.Parse(root.Element("connection").Element("smallFileLimit").Value);
                SocketSendTimeOut = int.Parse(root.Element("connection").Element("socketSendTimeout").Value);
                SocketReceiveTimeOut = int.Parse(root.Element("connection").Element("socketReceiveTimeout").Value);
            }
        }
    }
}
