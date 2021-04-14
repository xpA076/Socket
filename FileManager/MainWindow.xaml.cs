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
using FileManager.Static;
using SocketLib;
using FileManager.Models;
using FileManager.ViewModels;
using System.Net.Sockets;

namespace FileManager
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string host = "http://server.hhcsdtc.com:9999";
        private const string exeName = "SocketFileManager";

        private TCPAddress _server_address = null;

        public TCPAddress ServerAddress
        {
            get
            {
                return _server_address;
            }
            set
            {
                /// 每次IP变动都改动在 MainWindow 下的 ServerAddress
                /// 同时在此触发各类 UI 和其它类同步更改
                _server_address = value;
                this.SubPageBrowser.SetConnectedIPText(value);
                SocketFactory.TcpAddress = value;
            }
        }



        System.Windows.Forms.NotifyIcon notifyIcon = null;

        private Dictionary<string, object> pages;

        private HeartBeatConnectionMonitor connectionMonitor = new HeartBeatConnectionMonitor();

        private readonly ConnectionStatusViewModel ConnectionStatusView = new ConnectionStatusViewModel();

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

        public MainWindow()
        {
            // Load configurations
            Config.LoadConfig();

            // todo
            string[] args = Environment.GetCommandLineArgs();

            InitializeComponent();
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            //CheckUpdate();
            // NotifyIcon
            // https://blog.csdn.net/zhumingyan/article/details/51136690
            this.notifyIcon = new System.Windows.Forms.NotifyIcon();
            this.notifyIcon.Text = "FileManager";
            using (MemoryStream ms = new MemoryStream(),
                msPng = new MemoryStream(),
                msIco = new MemoryStream())
            {
                // 通过内存流转码, 将 Image 控件的 ImageSource 图像作为 NotifyIcon 的 Icon 图标
                // https://blog.csdn.net/u012790747/article/details/48086949
                // https://www.jb51.net/article/110155.htm
                // https://blog.csdn.net/qq_18995513/article/details/53693554

                // 从名为 TitleIcon 的 Image 控件的 ImageSource 获取 Bitmap, 在内存流中转为PNG格式
                BitmapSource bmpSrc = (BitmapSource)this.TitleIcon.Source;
                Bitmap bmp = new Bitmap(bmpSrc.PixelWidth, bmpSrc.PixelHeight, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
                BitmapData data = bmp.LockBits(
                    new System.Drawing.Rectangle(System.Drawing.Point.Empty, bmp.Size),
                    System.Drawing.Imaging.ImageLockMode.WriteOnly,
                    System.Drawing.Imaging.PixelFormat.Format32bppPArgb
                );
                bmpSrc.CopyPixels(Int32Rect.Empty, data.Scan0, data.Height * data.Stride, data.Stride);
                bmp.UnlockBits(data);
                bmp.Save(msPng, ImageFormat.Png);
                // 将 Png 格式数据流写入 Icon 中
                BinaryWriter bin = new BinaryWriter(msIco);
                // 写图标头部
                bin.Write((short)0);            //0-1保留
                bin.Write((short)1);            //2-3文件类型。1=图标, 2=光标
                bin.Write((short)1);            //4-5图像数量（图标可以包含多个图像）
                bin.Write((byte)bmp.Width);     //6图标宽度
                bin.Write((byte)bmp.Height);    //7图标高度
                bin.Write((byte)0);             //8颜色数（若像素位深>=8，填0。这是显然的，达到8bpp的颜色数最少是256，byte不够表示）
                bin.Write((byte)0);             //9保留。必须为0
                bin.Write((short)0);            //10-11调色板
                bin.Write((short)32);           //12-13位深
                bin.Write((int)msPng.Length);   //14-17位图数据大小
                bin.Write(22);                  //18-21位图数据起始字节
                // 写图像数据
                bin.Write(msPng.ToArray());
                bin.Flush();
                bin.Seek(0, SeekOrigin.Begin);
                System.Drawing.Icon ico = new Icon(msIco);
                this.notifyIcon.Icon = ico;
            }
            this.notifyIcon.Visible = true;
            System.Windows.Forms.MenuItem exit = new System.Windows.Forms.MenuItem("Exit");
            exit.Click += new EventHandler(CloseWindow);
            System.Windows.Forms.MenuItem[] childen = new System.Windows.Forms.MenuItem[] { exit };
            this.notifyIcon.ContextMenu = new System.Windows.Forms.ContextMenu(childen);
            notifyIcon.MouseDoubleClick += ShowWindow;

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
                System.Windows.Forms.MessageBox.Show("please stop downloading first");
            }
            else
            {
                //this.notifyIcon.Visible = false;
                this.notifyIcon.Dispose();
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
            if (Config.ClickCloseToMinimize)
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

        public void RedirectPage(string pageName)
        {
            this.SidebarConnect.Background = this.SidebarGrid.Background;
            this.SidebarBrowser.Background = this.SidebarGrid.Background;
            this.SidebarTransfer.Background = this.SidebarGrid.Background;
            this.SidebarCode.Background = this.SidebarGrid.Background;
            this.SidebarSettings.Background = this.SidebarGrid.Background;
            this.SidebarServer.Background = this.SidebarGrid.Background;
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
            }
            block.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 30, 30));
            this.MainContent.Content = new Frame() { Content = this.pages[pageName] };
        }

        #endregion


        public void StartConnectionMonitor()
        {
            connectionMonitor.Init();
            connectionMonitor.HeartBeatUnitCallback = (() =>
            {
                ConnectionStatusView.SetStatus(connectionMonitor);
            });
            connectionMonitor.StartHeartBeat();
        }

        public void StopConnectionMonitor()
        {
            connectionMonitor.StopHeartBeat();
        }


        private void CheckUpdate()
        {
            string updaterPath = System.IO.Path.Combine(
                System.IO.Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName),
                "Updater.exe");
            try
            {
                if (File.Exists(updaterPath))
                {
                    File.Delete(updaterPath);
                }
            }
            catch (Exception) {; }
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(host + "/Echo/CheckVersion");
                // 发送请求
                request.Method = "POST";
                request.ContentType = "application/json;charset=UTF-8";
                var byteData = Encoding.UTF8.GetBytes(exeName);
                var length = byteData.Length;
                request.ContentLength = length;
                var writer = request.GetRequestStream();
                writer.Write(byteData, 0, length);
                writer.Close();
                // 接收数据
                var response = (HttpWebResponse)request.GetResponse();
                string responseString = new StreamReader(response.GetResponseStream(), Encoding.GetEncoding("utf-8")).ReadToEnd();
                // 获取版本号
                Version latestVersion = new Version(responseString);
                Version currentVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                if (latestVersion > currentVersion)
                {
                    // 下载Updater
                    FileStream fileStream = new FileStream(updaterPath, FileMode.Create);
                    HttpWebRequest fileRequest = (HttpWebRequest)WebRequest.Create(host + "/File/Download/Updater.exe");
                    WebResponse fileResponse = fileRequest.GetResponse();
                    Stream fileResponseStream = fileResponse.GetResponseStream();
                    byte[] bytes = new byte[1024];
                    int size = fileResponseStream.Read(bytes, 0, bytes.Length);
                    while (size > 0)
                    {
                        fileStream.Write(bytes, 0, size);
                        size = fileResponseStream.Read(bytes, 0, bytes.Length);
                    }
                    fileStream.Close();
                    fileResponseStream.Close();
                    // Updater -name AhkSetter
                    Process proc = new Process();
                    ProcessStartInfo startInfo = new ProcessStartInfo();
                    startInfo.FileName = updaterPath;
                    startInfo.Arguments = "-name " + exeName;
                    Process.Start(startInfo);
                    this.Close();
                }
            }
            catch (Exception) {; }
        }

    }
}
