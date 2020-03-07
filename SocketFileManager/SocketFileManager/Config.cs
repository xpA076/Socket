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
        public static int ServerDownloadPort { get; private set; }
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
                ServerDownloadPort = 12139;
                // create xml donfig
                XDocument doc = new XDocument();
                XElement root = new XElement("socketFileManagerConfig");
                XElement server = new XElement("server");
                server.SetElementValue("serverPort", ServerPort);
                server.SetElementValue("serverDownloadPort", ServerDownloadPort);
                root.Add(server);
                XElement connection = new XElement("connection");
                connection.SetElementValue("lastConnect", "");
                root.Add(connection);
                root.Save(configPath);
            }
            else
            {
                XDocument doc = XDocument.Load(configPath);
                XElement root = doc.Root;
                ServerPort = int.Parse(root.Element("server").Element("serverPort").Value);
                ServerDownloadPort = int.Parse(root.Element("server").Element("serverDownloadPort").Value);
            }
        }
    }
}
