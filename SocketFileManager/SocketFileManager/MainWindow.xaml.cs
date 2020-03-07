using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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

using SocketFileManager.Pages;
using SocketFileManager.SocketLib;

namespace SocketFileManager
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.Topbar.MouseDown += new MouseButtonEventHandler(Topbar_MouseDown);
            this.WindowMinimize.PreviewMouseLeftButtonDown += new MouseButtonEventHandler(WindowMinimize_MouseLeftDown);
            this.WindowClose.PreviewMouseLeftButtonDown += new MouseButtonEventHandler(WindowClose_MouseLeftDown);
            // Sidebar
            this.SidebarConnect.PreviewMouseLeftButtonDown += new MouseButtonEventHandler(SidebarConnect_MouseLeftDown);
            this.SidebarBrowser.PreviewMouseLeftButtonDown += new MouseButtonEventHandler(SidebarBrowser_MouseLeftDown);
            this.SidebarDownload.PreviewMouseLeftButtonDown += new MouseButtonEventHandler(SidebarDownload_MouseLeftDown);
            this.SidebarCode.PreviewMouseLeftButtonDown += new MouseButtonEventHandler(SidebarCode_MouseLeftDown);
            this.SidebarServer.PreviewMouseLeftButtonDown += new MouseButtonEventHandler(SidebarServer_MouseLeftDown);
            // Pages
            this.pages = new Dictionary<string, object>()
            {
                { "Connect", new PageConnect(this) },
                { "Browser", new PageBrowser(this) },
                { "Download", new PageDownload() },
                { "Code", new PageCode() },
            };
            RedirectPage("Connect");
            // Load configurations
            Config.LoadConfig();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var a = this.Icon;
            this.TitleIcon.Source = this.Icon;
            //RedirectPage("Connect");
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

        public void SetTitle(string title)
        {
            this.Title.Text = title;
        }
        #endregion

        #region 侧边栏行为
        private Dictionary<string, object> pages;
        private void SidebarConnect_MouseLeftDown(object sender, MouseButtonEventArgs e)
        {
            RedirectPage("Connect");
        }
        private void SidebarBrowser_MouseLeftDown(object sender, MouseButtonEventArgs e)
        {
            RedirectPage("Browser");
        }
        private void SidebarDownload_MouseLeftDown(object sender, MouseButtonEventArgs e)
        {
            RedirectPage("Download");
        }
        private void SidebarCode_MouseLeftDown(object sender, MouseButtonEventArgs e)
        {
            RedirectPage("Code");
        }
        private void SidebarServer_MouseLeftDown(object sender, MouseButtonEventArgs e)
        {
            Window server = new ServerWindow();
            server.Show();
            this.Close();
        }

        public void RedirectPage(string pageName)
        {
            this.SidebarConnect.Background = this.SidebarGrid.Background;
            this.SidebarBrowser.Background = this.SidebarGrid.Background;
            this.SidebarDownload.Background = this.SidebarGrid.Background;
            this.SidebarCode.Background = this.SidebarGrid.Background;
            TextBlock block = null;
            switch (pageName)
            {
                case "Connect":
                    block = this.SidebarConnect;
                    break;
                case "Browser":
                    block = this.SidebarBrowser;
                    break;
                case "Download":
                    block = this.SidebarDownload;
                    break;
                case "Code":
                    block = this.SidebarCode;
                    break;
            }
            block.Background = new SolidColorBrush(Color.FromRgb(30, 30, 30));
            this.MainContent.Content = new Frame() { Content = this.pages[pageName] };
        }
        #endregion


        public IPAddress ServerIP { get; set; } = null;
        public int serverPort;

        public void ListFiles()
        {
            ((PageBrowser)this.pages["Browser"]).ListFiles();
        }

    }
}
;