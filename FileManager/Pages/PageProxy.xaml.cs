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
using FileManager.Events;
using FileManager.SocketLib.Enums;
using FileManager.SocketLib.SocketProxy;
using FileManager.SocketLib.SocketServer;
using FileManager.Static;
using FileManager.ViewModels;

namespace FileManager.Pages
{
    /// <summary>
    /// PageProxy.xaml 的交互逻辑
    /// </summary>
    public partial class PageProxy : Page
    {
        private ServerRichTextBoxViewModel RichTextBoxView = new ServerRichTextBoxViewModel();


        public PageProxy()
        {
            InitializeComponent();
            this.TextBoxPort.Text = Config.DefaultProxyPort.ToString();
            RichTextBoxView.RichTextBoxUpdate += RichTextBoxLog_OnUpdate;
            this.TextBoxNull.DataContext = RichTextBoxView;
            //ButtonStartProxy_Click(null, null);
        }

        private void ButtonStartProxy_Click(object sender, RoutedEventArgs e)
        {
            this.ButtonStartProxy.Visibility = Visibility.Hidden;
            IPAddress host = Dns.GetHostAddresses(Dns.GetHostName()).Where(ip =>
                ip.AddressFamily == AddressFamily.InterNetwork && !ip.ToString().StartsWith("172")).FirstOrDefault();
            SocketProxy proxy = new SocketProxy(host);
            proxy.SocketLog += Proxy_OnLog;
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
                Proxy_OnLog(this, new SocketLogEventArgs("Proxy window start listening error: " + ex.Message, LogLevel.Error));
            }

        }

        private void ButtonClear_Click(object sender, RoutedEventArgs e)
        {
            this.RichTextBoxLog.Document.Blocks.Clear();
        }


        private void Proxy_OnLog(object sender, SocketLogEventArgs e)
        {
            if ((int)e.logLevel > (int)LogLevel.Info)
            {
                //return;
            }
            Logger.ServerLog(e.log, e.logLevel, e.time);
            this.RichTextBoxView.InvokeLog(e);
        }


        private void RichTextBoxLog_OnUpdate(object sender, SocketLogEventArgs e)
        {
            SolidColorBrush b1 = new SolidColorBrush(Colors.White);
            SolidColorBrush b2 = new SolidColorBrush(Colors.White);
            switch (e.logLevel)
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
            p.Inlines.Add(new Run() { Text = e.time.ToString("yyyy-MM-dd HH:mm:ss.fff "), Foreground = b1 });
            p.Inlines.Add(new Run() { Text = "[" + e.logLevel.ToString().PadRight(5) + "] ", Foreground = b2 });
            p.Inlines.Add(new Run() { Text = e.log, Foreground = b1 });
            this.RichTextBoxLog.Document.Blocks.Add(p);
            this.RichTextBoxLog.UpdateLayout();
        }


    }
}
