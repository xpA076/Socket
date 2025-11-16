using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
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

using FileManager.Pages;
using FileManager.ViewModels;
using System.Net.Sockets;
using FileManager.Models.SocketLib.Models;
using FileManager.Models.Config;
using Microsoft.Extensions.DependencyInjection;
using FileManager.Models.HeartBeatLib;

namespace FileManager
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class FileManagerMainWindow : Window
    {
        private const string exeName = "SocketFileManager";

        private readonly ConfigService configService = Program.Provider.GetService<ConfigService>();


        // *** todo ***
        public bool IsConnected
        {
            get
            {
                return CurrentRoute == null;
            }
        }

        public ConnectionRoute CurrentRoute
        {
            get; set;
        }


        private Dictionary<string, object> pages;

        private HeartBeatConnectionMonitor connectionMonitor = new HeartBeatConnectionMonitor();

        private readonly ConnectionStatusViewModel ConnectionStatusView = new ConnectionStatusViewModel();

        #region SubPage

        public PageConnect SubPageConnect
        {
            get
            {
                return (PageConnect)this.pages["Connect"];
            }
        }


        public PageBrowser SubPageBrowser
        {
            get
            {
                return (PageBrowser)this.pages["Browser"];
            }
        }

        public PageTransfer SubPageTransfer
        {
            get
            {
                return (PageTransfer)this.pages["Transfer"];
            }
        }


        public PageCode SubPageCode
        {
            get
            {
                return (PageCode)this.pages["Code"];
            }
        }

        public PageSettings SubPageSettings
        {
            get
            {
                return (PageSettings)this.pages["Settings"];
            }
        }

        public PageServer SubPageServer
        {
            get
            {
                return (PageServer)this.pages["Server"];
            }
        }
        #endregion

        public FileManagerMainWindow()
        {

            // Load configurations

            // todo
            string[] args = Environment.GetCommandLineArgs();

            InitializeComponent();
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;

            // Topbar
            this.Topbar.MouseDown += new MouseButtonEventHandler(Topbar_MouseDown);
            this.WindowMinimize.PreviewMouseLeftButtonDown += new MouseButtonEventHandler(MinimizeWindow);
            this.WindowClose.PreviewMouseLeftButtonDown += new MouseButtonEventHandler(Topbar_RedCrossClick);
            this.ButtonStatusSymbol.DataContext = ConnectionStatusView;
            // Sidebar
            /*
            this.SidebarConnect.PreviewMouseLeftButtonDown += new MouseButtonEventHandler(SidebarConnect_MouseLeftDown);
            this.SidebarBrowser.PreviewMouseLeftButtonDown += new MouseButtonEventHandler(SidebarBrowser_MouseLeftDown);
            this.SidebarTransfer.PreviewMouseLeftButtonDown += new MouseButtonEventHandler(SidebarTransfer_MouseLeftDown);
            this.SidebarCode.PreviewMouseLeftButtonDown += new MouseButtonEventHandler(SidebarCode_MouseLeftDown);
            this.SidebarSettings.PreviewMouseLeftButtonDown += new MouseButtonEventHandler(SidebarSettings_MouseLeftDown);
            */
            // Pages
            this.pages = new Dictionary<string, object>()
            {
                { "Connect", new PageConnect(this) },
                { "Browser", new PageBrowser(this) },
                { "Transfer", new PageTransfer(this) },
                { "Code", new PageCode() },
                { "Settings", new PageSettings() },
                { "Server", new PageServer() },
                { "Proxy", new PageProxy() },
            };
            RedirectPage("Connect");
        }


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.TitleIcon.Source = this.Icon;
            //RedirectPage("Connect");
        }

        #region 标题栏行为
        private void CloseWindow(object sender, EventArgs e)
        {
            if (((PageTransfer)this.pages["Transfer"]).IsTransfering)
            {
                //System.Windows.Forms.MessageBox.Show("please stop downloading first");
                this.Close();
            }
            else
            {
                //this.notifyIcon.Visible = false;
                this.Close();
            }
        }
        private void ShowWindow(object sender, EventArgs e)
        {
            if (this.Visibility == Visibility.Hidden)
            {
                this.Show();
            }
            else
            {
                this.WindowState = WindowState.Normal;
                this.Activate();
            }
        }
        private void MinimizeWindow(object sender, EventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }
        private void Topbar_RedCrossClick(object sender, EventArgs e)
        {
            if (configService.ClickCloseToMinimize)
            {
                this.Hide();
            }
            else
            {
                CloseWindow(null, null);
            }
            
        }
        private void Topbar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }
        #endregion

        #region 侧边栏行为
        
        private void SidebarConnect_MouseLeftDown(object sender, MouseButtonEventArgs e)
        {
            RedirectPage("Connect");
        }
        private void SidebarBrowser_MouseLeftDown(object sender, MouseButtonEventArgs e)
        {
            RedirectPage("Browser");
        }
        private void SidebarTransfer_MouseLeftDown(object sender, MouseButtonEventArgs e)
        {
            RedirectPage("Transfer");
        }
        private void SidebarCode_MouseLeftDown(object sender, MouseButtonEventArgs e)
        {
            RedirectPage("Code");
        }
        private void SidebarSettings_MouseLeftDown(object sender, MouseButtonEventArgs e)
        {
            RedirectPage("Settings");
        }
        private void SidebarServer_MouseLeftDown(object sender, MouseButtonEventArgs e)
        {
            RedirectPage("Server");
        }
        private void SidebarProxy_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            RedirectPage("Proxy");
        }


        public void RedirectPage(string pageName)
        {
            this.SidebarConnect.Background = this.SidebarGrid.Background;
            this.SidebarBrowser.Background = this.SidebarGrid.Background;
            this.SidebarTransfer.Background = this.SidebarGrid.Background;
            this.SidebarCode.Background = this.SidebarGrid.Background;
            this.SidebarSettings.Background = this.SidebarGrid.Background;
            this.SidebarServer.Background = this.SidebarGrid.Background;
            this.SidebarProxy.Background = this.SidebarGrid.Background;
            TextBlock block = null;
            switch (pageName)
            {
                case "Connect":
                    block = this.SidebarConnect;
                    break;
                case "Browser":
                    block = this.SidebarBrowser;
                    break;
                case "Transfer":
                    block = this.SidebarTransfer;
                    break;
                case "Code":
                    block = this.SidebarCode;
                    break;
                case "Settings":
                    block = this.SidebarSettings;
                    break;
                case "Server":
                    block = this.SidebarServer;
                    break;
                case "Proxy":
                    block = this.SidebarProxy;
                    break;
            }
            block.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 30, 30));
            this.MainContent.Content = new Frame() { Content = this.pages[pageName] };
        }

        #endregion


        public void StartConnectionMonitor()
        {
            connectionMonitor.Init();
            connectionMonitor.HeartBeatUnitCallback += (object sender, EventArgs e) =>
            {
                ConnectionStatusView.SetStatus(sender as HeartBeatConnectionMonitor);
            };
            connectionMonitor.StartHeartBeat();
        }

        public void StopConnectionMonitor()
        {
            connectionMonitor.StopHeartBeat();
        }


        private void SidebarReverseProxy_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {

        }
    }
}
