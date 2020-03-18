using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SocketServerConsole
{
    public static class Config
    {
        public static int ServerPort { get; private set; }

        public static int SocketSendTimeOut { get; set; } = 3000;

        public static int SocketReceiveTimeOut { get; set; } = 3000;


        private static string configPath
        {
            get
            {
                string dir = System.Environment.CurrentDirectory;
                return System.Environment.CurrentDirectory + "\\SocketServerConsole.config";
            }
        }

        public static void LoadConfig()
        {
            if (!File.Exists(configPath))
            {
                // default config
                ServerPort = 12138;
                // create xml donfig
                XDocument doc = new XDocument();
                XElement root = new XElement("socketFileManagerConfig");
                XElement server = new XElement("server");
                server.SetElementValue("serverPort", ServerPort);
                root.Add(server);
                XElement connection = new XElement("connection");
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
                SocketSendTimeOut = int.Parse(root.Element("connection").Element("socketSendTimeout").Value);
                SocketReceiveTimeOut = int.Parse(root.Element("connection").Element("socketReceiveTimeout").Value);
            }
        }
    }
}
