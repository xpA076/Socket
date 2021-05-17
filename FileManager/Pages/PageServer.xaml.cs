using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.Net;
using System.ComponentModel;
using System.Net.Sockets;
using System.IO;

using FileManager.SocketLib;
using FileManager.SocketLib.SocketServer;
using FileManager.Static;
using FileManager.SocketLib.Enums;

namespace FileManager.Pages
{
    /// <summary>
    /// PageServer.xaml 的交互逻辑
    /// </summary>
    public partial class PageServer : Page
    {
        private PageServerViewModel PageServerView = new PageServerViewModel();


        public PageServer()
        {
            InitializeComponent();
            this.TextBoxPort.Text = Config.DefaultServerPort.ToString();
            /// Log UI 更新对应 delegate
            PageServerView.RichTextBoxUpdate = (time, logLevel, log) =>
            {
                SolidColorBrush b1 = new SolidColorBrush(Colors.White);
                SolidColorBrush b2 = new SolidColorBrush(Colors.White);
                switch (logLevel)
                {
                    case LogLevel.Error:
                        b2 = new SolidColorBrush(Color.FromRgb(255, 192, 192));
                        break;
                    case LogLevel.Warn:
                        b2 = new SolidColorBrush(Colors.LightYellow);
                        break;
                    case LogLevel.Info:
                        b2 = new SolidColorBrush(Color.FromRgb(0, 127, 255));
                        break;
                }
                Paragraph p = new Paragraph();
                p.Inlines.Add(new Run() { Text = time.ToString("yyyy-MM-dd HH:mm:ss.fff "), Foreground = b1 });
                p.Inlines.Add(new Run() { Text = "[" + logLevel.ToString().PadRight(5) + "] ", Foreground = b2 });
                p.Inlines.Add(new Run() { Text = log, Foreground = b1 });
                this.RichTextBoxLog.Document.Blocks.Add(p);
                this.RichTextBoxLog.UpdateLayout();
            };
            this.TextBoxNull.DataContext = PageServerView;
            //ButtonStartListen_Click(null, null);
        }


        private void ButtonStartListen_Click(object sender, RoutedEventArgs e)
        {
            this.ButtonStartListen.Visibility = Visibility.Hidden;
            IPAddress host = Dns.GetHostAddresses(Dns.GetHostName()).Where(ip =>
                ip.AddressFamily == AddressFamily.InterNetwork && !ip.ToString().StartsWith("172")).FirstOrDefault();
            SocketServer server = new SocketServer(host);
            if (!File.Exists(Config.ServerConfigPath))
            {
                server.Config.Create(Config.ServerConfigPath);
            }
            server.Config.Load(Config.ServerConfigPath);
            server.Logger = ServerLogger;
            server.CheckIdentity = CheckIdentity;
            try
            {
                int port = int.Parse(this.TextBoxPort.Text);
                server.InitializeServer(port);
                server.StartListening();
            }
            catch (Exception ex)
            {
                server.Close();
                //MessageBox.Show("Server window start listening error: " + ex.Message);
                ServerLogger("Server window start listening error: " + ex.Message, LogLevel.Error);
            }
        }



        private void ButtonClear_Click(object sender, RoutedEventArgs e)
        {
            this.RichTextBoxLog.Document.Blocks.Clear();
        }


        private SocketLib.Enums.SocketIdentity CheckIdentity(HB32Header header, byte[] bytes)
        {
            return SocketLib.Enums.SocketIdentity.All;
        }


        private void ServerLogger(string log, LogLevel logLevel)
        {
            if ((int)logLevel > (int)LogLevel.Info)
            {
                //return;
            }
            DateTime now = DateTime.Now;
            Logger.ServerLog(log, logLevel, now);
            this.PageServerView.InvokeLog(now, logLevel, log);
        }


        internal delegate void RichTextBoxUpdateHandler(DateTime time, LogLevel logLevel, string log);


        /// <summary>
        /// 用这种不太优雅的方式实现 RichTextBox 界面更新
        /// 因为 SocketServer 的 Log 操作基本都在新建线程中, 没有在 WPF 更新 UI 的权限
        /// 通过数据绑定触发UI更新事件, 在UI更新线程中通过delegate更新 RichTextBox 中的 FlowDocument
        /// </summary>
        internal class PageServerViewModel : INotifyPropertyChanged
        {
            public RichTextBoxUpdateHandler RichTextBoxUpdate;
            public event PropertyChangedEventHandler PropertyChanged;
            
            public string Nothing
            {
                get
                {
                    RichTextBoxUpdate(_time, _logLevel, _log);
                    return "";
                }
            }

            DateTime _time = DateTime.Now;
            LogLevel _logLevel = LogLevel.Info;
            string _log = "Init";

            public void InvokeLog(DateTime time, LogLevel logLevel, string log)
            {
                _time = time;
                _logLevel = logLevel;
                _log = log;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Nothing"));
            }
        }
    }
}
