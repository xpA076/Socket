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

using SocketFileManager.Pages;

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

        #region 侧边栏行为
        private Dictionary<string, Page> pages = new Dictionary<string, Page>()
        {
            { "Connect", new PageConnect() },
            { "Browser", new PageBrowser() },
            { "Download", new PageDownload() },
            { "Code", new PageCode() },
        };
        private void SidebarConnect_MouseLeftDown(object sender, MouseButtonEventArgs e)
        {
            SidebarFocus(this.SidebarConnect);
            this.MainContent.Content = new Frame() { Content = pages["Connect"] };
        }
        private void SidebarBrowser_MouseLeftDown(object sender, MouseButtonEventArgs e)
        {
            SidebarFocus(this.SidebarBrowser);
            this.MainContent.Content = new Frame() { Content = pages["Browser"] };
        }
        private void SidebarDownload_MouseLeftDown(object sender, MouseButtonEventArgs e)
        {
            SidebarFocus(this.SidebarDownload);
            this.MainContent.Content = new Frame() { Content = pages["Download"] };
        }
        private void SidebarCode_MouseLeftDown(object sender, MouseButtonEventArgs e)
        {
            SidebarFocus(this.SidebarCode);
            this.MainContent.Content = new Frame() { Content = pages["Code"] };
        }
        private void SidebarServer_MouseLeftDown(object sender, MouseButtonEventArgs e)
        {
            //SidebarFocus(this.SidebarCode);
            //this.MainContent.Content = new Frame() { Content = pages["Code"] };
            Window server = new ServerWindow();
            server.Show();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            SidebarConnect_MouseLeftDown(null, null);
        }

        private void SidebarFocus(TextBlock block)
        {
            this.SidebarConnect.Background = this.SidebarGrid.Background;
            this.SidebarBrowser.Background = this.SidebarGrid.Background;
            this.SidebarDownload.Background = this.SidebarGrid.Background;
            this.SidebarCode.Background = this.SidebarGrid.Background;
            block.Background = this.MainGrid.Background;
        }
        #endregion
    }
}
;