using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace StatusReporter
{
    public static class ReporterConfig
    {
        public static string ClientName { get; set; } = "Unknown";

        public static IPAddress ServerIP { get; set; }

        public static int ServerPort { get; set; } = 12138;

        public static int Interval { get; set; } = 60000;

        private static string ConfigPath
        {
            get
            {
                return Application.StartupPath + "\\StatusReporter.config";
            }
        }


        public static bool CheckConfig()
        {
            return File.Exists(ConfigPath);
        }


        public static void CreateConfig()
        {
            ServerIP = IPAddress.Parse("222.195.95.231");
            SaveConfig();
        }

        public static void LoadConfig()
        {
            XDocument doc = XDocument.Load(ConfigPath);
            XElement root = doc.Root;
            ClientName = root.Element("ClientName").Value;
            ServerIP = IPAddress.Parse(root.Element("ServerIP").Value);
            ServerPort = int.Parse(root.Element("ServerPort").Value);
            Interval = int.Parse(root.Element("Interval").Value);
        }

        public static void SaveConfig()
        {
            XElement root = new XElement("StatusReporterConfig");
            root.SetElementValue("ClientName", ClientName);
            root.SetElementValue("ServerIP", ServerIP);
            root.SetElementValue("ServerPort", ServerPort);
            root.SetElementValue("Interval", Interval);
            root.Save(ConfigPath);
        }

    }
}
