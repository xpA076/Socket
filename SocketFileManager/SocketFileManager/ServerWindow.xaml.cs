using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using SocketFileManager.SocketLib;


namespace SocketFileManager
{
    /// <summary>
    /// ServerWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ServerWindow : Window
    {
        public ServerWindow()
        {
            InitializeComponent();
            this.Topbar.MouseDown += new MouseButtonEventHandler(Topbar_MouseDown);
            this.WindowMinimize.PreviewMouseLeftButtonDown += new MouseButtonEventHandler(WindowMinimize_MouseLeftDown);
            this.WindowClose.PreviewMouseLeftButtonDown += new MouseButtonEventHandler(WindowClose_MouseLeftDown);
        }

        #region 标题栏行为
        private void Topbar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void WindowMinimize_MouseLeftDown(object sender, MouseButtonEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void WindowClose_MouseLeftDown(object sender, MouseButtonEventArgs e)
        {
            this.Close();
        }
        #endregion

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //int port = int.Parse(System.Configuration.ConfigurationManager.AppSettings["serverPort"]);
            string name = Dns.GetHostName();
            IPAddress host = Dns.GetHostAddresses(Dns.GetHostName()).
                Where(ip => ip.AddressFamily == AddressFamily.InterNetwork).
                FirstOrDefault();
            this.Text.Text = string.Format("Working as server ...\nIP address: {0}\nPort num: {1}", 
                host.ToString(), Config.ServerPort.ToString());

            SocketServer s;
            s = new SocketServer(host, Config.ServerPort);
            try
            {
                //s.InitializeServer();
                // 绑定端口，启动listen
                IPEndPoint ipe = new IPEndPoint(host, Config.ServerPort);
                s.server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                s.server.Bind(ipe);
                //s.server.SendTimeout = 3000;
                //s.server.ReceiveTimeout = 3000;
                s.server.Listen(20);
                // 从主线程创建监听线程
                //s.StartListen();
                Thread th_listen = new Thread(s.ServerListen);
                th_listen.IsBackground = true;
                th_listen.Start();

            }
            catch (Exception ex)
            {
                s.Close();
                MessageBox.Show("Server window start listening error: " + ex.Message);
            }
        }
    }
}
