using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using SocketLib;
using SocketLib.SocketServer;

namespace FileManagerServer
{
    public partial class ServerForm : Form
    {
        private string ConfigPath
        {
            get
            {
                return Application.StartupPath + "\\ServerForm.config";
            }
        }

        private string LogPath
        {
            get
            {
                return Application.StartupPath + "\\ServerForm.log";
            }
        }


        public ServerForm()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.richTextBox1.Clear();
        }

        private void ServerForm_Load(object sender, EventArgs e)
        {

            IPAddress host = Dns.GetHostAddresses(Dns.GetHostName()).Where(ip =>
                ip.AddressFamily == AddressFamily.InterNetwork && !ip.ToString().StartsWith("172")).FirstOrDefault();

            SocketServer server = new SocketServer(host);
            if (!File.Exists(ConfigPath))
            {
                server.Config.Create(ConfigPath);
            }
            server.Logger = ServerFormLogger;
            server.Config.Load(ConfigPath);
            try
            {
                server.InitializeServer();
                server.StartListening();
            }
            catch (Exception ex)
            {
                server.Close();
                //MessageBox.Show("Server window start listening error: " + ex.Message);
                ServerFormLogger("Server window start listening error: " + ex.Message, LogLevel.Error);
            }
        }

        private void ServerFormLogger(string log, LogLevel logLevel)
        {
            if ((int)logLevel > (int)LogLevel.Info)
            {
                return;
            }
            string time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string logLevel_str = "[" + logLevel.ToString().PadRight(5) + "]";
            /// log in file
            using (FileStream stream = new FileStream(LogPath, FileMode.Append))
            using (StreamWriter writer = new StreamWriter(stream))
            {
                writer.WriteLine("{0} {1} {2}", time, logLevel_str, log);
            }

            /// log in GUI
            this.BeginInvoke(new Action(() =>
            {
                /// 将光标位置设置到当前内容的末尾
                this.richTextBox1.SelectionStart = this.richTextBox1.Text.Length;
                this.richTextBox1.SelectionLength = 0;
                /// 滚动到光标位置
                this.richTextBox1.ScrollToCaret();

                /// add text
                this.richTextBox1.AppendText(time + " ");
                switch (logLevel)
                {
                    case LogLevel.Error:
                        this.richTextBox1.SelectionColor = Color.Red;
                        break;
                    case LogLevel.Warn:
                        this.richTextBox1.SelectionColor = Color.DarkOrange;
                        break;
                    case LogLevel.Info:
                        this.richTextBox1.SelectionColor = Color.Blue;
                        break;
                }


                this.richTextBox1.AppendText(logLevel_str);
                this.richTextBox1.SelectionColor = this.richTextBox1.ForeColor;
                this.richTextBox1.AppendText(" " + log + "\n");
            }));
        }
    }
}
