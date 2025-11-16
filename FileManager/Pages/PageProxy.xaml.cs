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
using FileManager.Models.Config;
using FileManager.Models.Log;
using FileManager.Models.SocketLib;
using FileManager.Models.SocketLib.Enums;
using FileManager.Static;
using FileManager.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Color = System.Windows.Media.Color;

namespace FileManager.Pages
{
    /// <summary>
    /// PageProxy.xaml 的交互逻辑
    /// </summary>
    public partial class PageProxy : Page
    {
        private LogService logService = Program.Provider.GetService<LogService>();

        private ConfigService configService = Program.Provider.GetService<ConfigService>();

        private ServerRichTextBoxViewModel RichTextBoxView = new ServerRichTextBoxViewModel();


        public PageProxy()
        {
            InitializeComponent();
            this.TextBoxPort.Text = configService.DefaultProxyPort.ToString();
            RichTextBoxView.RichTextBoxUpdate += RichTextBoxLog_OnUpdate;
            this.TextBoxNull.DataContext = RichTextBoxView;
            //ButtonStartProxy_Click(null, null);
        }

        private void ButtonStartProxy_Click(object sender, RoutedEventArgs e)
        {


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
            logService.ServerLog(e.log, e.logLevel, e.time);
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
