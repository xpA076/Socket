using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SocketLib.SocketServer
{
    public class SocketServerConfig
    {

        public int ServerPort { get; set; } = 12138;

        public int SocketSendTimeOut { get; set; } = 10000;

        public int SocketReceiveTimeOut { get; set; } = 10000;

        public int KeyLength { get; set; } = 16;

        public List<string> AllowDirectoryList { get; set; } = new List<string>()
        {
            @"C:",
            @"D:",
            @"E:",
            @"F:",
            @"G:",
            @"H:",
            @"I:",
        };

        public bool IsPathAllowed(string path)
        {
            return true;
        }


        public void Create(string configPath)
        {
            this.Save(configPath);
        }

        public void Load(string configPath)
        {
            XDocument doc = XDocument.Load(configPath);
            XElement root = doc.Root;
            this.ServerPort = int.Parse(root.Element("server").Element("serverPort").Value);
            this.SocketSendTimeOut = int.Parse(root.Element("connection").Element("socketSendTimeout").Value);
            this.SocketReceiveTimeOut = int.Parse(root.Element("connection").Element("socketReceiveTimeout").Value);
            this.AllowDirectoryList.Clear();
            foreach (XElement allowInfo in root.Element("allowList").Elements("directory"))
            {
                this.AllowDirectoryList.Add(allowInfo.Element("content").Value);
            }
        }

        public void Save(string configPath)
        {
            XElement root = new XElement("FileManagerServerConfig");
            XElement server = new XElement("server");
            server.SetElementValue("serverPort", this.ServerPort);
            root.Add(server);
            XElement connection = new XElement("connection");
            connection.SetElementValue("socketSendTimeout", this.SocketSendTimeOut.ToString());
            connection.SetElementValue("socketReceiveTimeout", this.SocketReceiveTimeOut.ToString());
            root.Add(connection);
            XElement allowList = new XElement("allowList");
            foreach (string path in this.AllowDirectoryList)
            {
                XElement dir = new XElement("directory");
                dir.SetElementValue("content", path);
                allowList.Add(dir);
            }
            root.Add(allowList);
            root.Save(configPath);
        }


    }
}
