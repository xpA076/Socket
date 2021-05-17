using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
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
using FileManager.SocketLib.Enums;
using FileManager.SocketLib.SocketProxy;
using FileManager.SocketLib.SocketServer;
using FileManager.Static;

namespace FileManager.Pages
{
    /// <summary>
    /// PageProxy.xaml 的交互逻辑
    /// </summary>
    public partial class PageProxy : Page
    {
        private PageProxyViewModel PageProxyView = new PageProxyViewModel();

        public PageProxy()
        {
            InitializeComponent();
            this.TextBoxPort.Text = Config.DefaultProxyPort.ToString();
            /// Log UI 更新对应 delegate
            PageProxyView.RichTextBoxUpdate = (time, logLevel, log) =>
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
            this.TextBoxNull.DataContext = PageProxyView;
        }

        private void ButtonStartProxy_Click(object sender, RoutedEventArgs e)
        {
            this.ButtonStartProxy.Visibility = Visibility.Hidden;
            IPAddress host = Dns.GetHostAddresses(Dns.GetHostName()).Where(ip =>
                ip.AddressFamily == AddressFamily.InterNetwork && !ip.ToString().StartsWith("172")).FirstOrDefault();
            SocketProxy proxy = new SocketProxy(host);
            proxy.Logger = ProxyLogger;
            try
            {
                int port = int.Parse(this.TextBoxPort.Text);
                proxy.InitializeServer(port);
                proxy.StartListening();
            }
            catch (Exception ex)
            {
                proxy.Close();
                //MessageBox.Show("Server window start listening error: " + ex.Message);
                ProxyLogger("Proxy window start listening error: " + ex.Message, LogLevel.Error);
            }

        }

        private void ButtonClear_Click(object sender, RoutedEventArgs e)
        {
            this.RichTextBoxLog.Document.Blocks.Clear();
        }


        private void ProxyLogger(string log, LogLevel logLevel)
        {
            if ((int)logLevel > (int)LogLevel.Info)
            {
                //return;
            }
            DateTime now = DateTime.Now;
            Logger.ServerLog(log, logLevel, now);
            this.PageProxyView.InvokeLog(now, logLevel, log);
        }


        internal delegate void RichTextBoxUpdateHandler(DateTime time, LogLevel logLevel, string log);


        /// <summary>
        /// 用这种不太优雅的方式实现 RichTextBox 界面更新
        /// 因为 SocketServer 的 Log 操作基本都在新建线程中, 没有在 WPF 更新 UI 的权限
        /// 通过数据绑定触发UI更新事件, 在UI更新线程中通过delegate更新 RichTextBox 中的 FlowDocument
        /// </summary>
        internal class PageProxyViewModel : INotifyPropertyChanged
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
