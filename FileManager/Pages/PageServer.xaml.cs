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
using FileManager.Events;
using FileManager.ViewModels;
using FileManager.Models;
using FileManager.SocketLib.SocketServer.Main;

namespace FileManager.Pages
{
    /// <summary>
    /// PageServer.xaml 的交互逻辑
    /// </summary>
    public partial class PageServer : Page
    {
        private ServerRichTextBoxViewModel RichTextBoxView = new ServerRichTextBoxViewModel();


        public PageServer()
        {
            InitializeComponent();
            this.TextBoxPort.Text = Config.Instance.DefaultServerPort.ToString();
            RichTextBoxView.RichTextBoxUpdate += RichTextBoxLog_OnUpdate;
            this.TextBoxNull.DataContext = RichTextBoxView;
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
            server.SocketLog += Server_OnLog;
            server.CheckIdentity += CheckIdentity;
            Logger.Server.InitServer();
            try
            {
                int port = int.Parse(this.TextBoxPort.Text);
                server.InitializeServer(port);
                server.StartListening();
            }
            catch (Exception ex)
            {
                server.Close();
                Server_OnLog(this, new SocketLogEventArgs("Server window start listening error: " + ex.Message, LogLevel.Error));
            }
        }



        private void ButtonClear_Click(object sender, RoutedEventArgs e)
        {
            this.RichTextBoxLog.Document.Blocks.Clear();
        }





        private void CheckIdentity(object sender, SocketIdentityCheckEventArgs e)
        {
            e.Identity = SocketIdentity.All;
        }


        private void Server_OnLog(object sender, SocketLogEventArgs e)
        {
            if ((int)e.logLevel > (int)LogLevel.Info)
            {
                //return;
            }
            LoggerStatic.ServerLog(e.log, e.logLevel, e.time);
            this.RichTextBoxView.InvokeLog(e);
        }


        private void RichTextBoxLog_OnUpdate(object sender, SocketLogEventArgs e)
        {
            SolidColorBrush b1 = new SolidColorBrush(Colors.White);
            SolidColorBrush b2 = new SolidColorBrush(Colors.White);
            switch (e?.logLevel)
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
                default:
                    return;
            }
            Paragraph p = new Paragraph();
            p.Inlines.Add(new Run() { Text = e.time.ToString("yyyy-MM-dd HH:mm:ss.fff "), Foreground = b1 });
            p.Inlines.Add(new Run() { Text = "[" + e.logLevel.ToString().PadRight(5) + "] ", Foreground = b2 });
            p.Inlines.Add(new Run() { Text = e.log, Foreground = b1 });
            this.RichTextBoxLog.Document.Blocks.Add(p);
            this.RichTextBoxLog.UpdateLayout();
            this.RichTextBoxLog.ScrollToEnd();
        }




    }
}
