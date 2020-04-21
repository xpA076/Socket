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
using System.Threading;

using SocketFileManager.SocketLib;

namespace SocketFileManager.Pages
{
    /// <summary>
    /// PageConnect.xaml 的交互逻辑
    /// </summary>
    public partial class PageConnect : Page
    {
        private MainWindow parent = null;

        public PageConnect()
        {
            InitializeComponent();
            this.ButtonConnect.PreviewMouseLeftButtonDown += new MouseButtonEventHandler(ButtonConnect_MouseLeftDown);
            this.TextIP.Text = Config.LastConnect;
        }


        public PageConnect(MainWindow parent):this()
        {
            this.parent = parent;
            //InitializeComponent();
        }

        private void ButtonConnect_MouseLeftDown(object sender, MouseButtonEventArgs e)
        {
            //parent.SidebarBrowser_MouseLeftDown(null,null);
            //MessageBox.Show(this.TextIP.Text);
            try
            {
                SocketClient s = new SocketClient(this.TextIP.Text, Config.ServerPort, (ex) => {
                    this.ButtonConnect.Dispatcher.BeginInvoke(new Action(()=> {
                        this.ButtonConnect.Content = "Connect";
                    }));
                    MessageBox.Show(ex.Message);
                });
                this.ButtonConnect.Content = "Connecting ...";
                s.AsyncConnect(()=> {
                    s.Close();
                    // 线程锁应该是lock(this), 所以所有this内部成员的访问都要通过Invoke进行
                    this.ButtonConnect.Dispatcher.BeginInvoke(new Action(() => {
                        Config.LastConnect = this.TextIP.Text;
                        this.parent.ServerIP = System.Net.IPAddress.Parse(this.TextIP.Text);
                        this.parent.ServerPort = Config.ServerPort;
                        this.ButtonConnect.Content = "Connect";
                        this.parent.Title.Text = "Connected IP : " + this.TextIP.Text;
                        this.parent.RedirectPage("Browser");
                        System.Threading.Thread.Sleep(100);
                        this.parent.ListFiles();
                    }));
                });
            }
            catch(Exception ex)
            {
                this.ButtonConnect.Content = "Connect";
                MessageBox.Show(ex.Message);
                return;
            }

        }
    }
}
