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

using SocketLib;
using SocketLib.Enums;
using FileManager.Windows;
using FileManager.Models;
using FileManager.Static;
using System.Net;
using System.Collections.ObjectModel;



namespace FileManager.Pages
{
    /// <summary>
    /// PageConnect.xaml 的交互逻辑
    /// </summary>
    public partial class PageConnect : Page
    {
        private MainWindow parent = null;


        private bool IsConnecting { get; set; } = false;

        private string _lastFocusListView = "";

        public TCPAddress ServerAddress
        {
            get
            {
                return this.parent.ServerAddress;
            }
            set
            {
                this.parent.ServerAddress = value;
            }
        }


        public PageConnect()
        {
            InitializeComponent();
            //this.ButtonConnect.PreviewMouseLeftButtonDown += new MouseButtonEventHandler(ButtonConnect_MouseLeftDown);
            if (Config.Histories.Count > 0)
            {
                this.TextIP.Text = Config.Histories[0].Info;
            }
            
            /*
            Histories.Add(new ConnectionRecord { Info = "114.214.176.136" });

            Histories.Add(new ConnectionRecord { Info = "127.123.456.255:12345" });
            Histories.Add(new ConnectionRecord { Info = "127.123.456.255:12385" });
            */
            this.ListViewHistory.ItemsSource = Config.Histories;
            this.ListViewStar.ItemsSource = Config.Stars;
            //Config.Stars.Add(new ConnectionRecord { Info = "127.123.456.255:12345" });
        }


        public PageConnect(MainWindow parent) : this()
        {
            this.parent = parent;
        }



        private void ListViewHistory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.ListViewHistory.SelectedIndex >= 0)
            {
                this.TextIP.Text = Config.Histories[this.ListViewHistory.SelectedIndex].Info;
            }
            
        }

        private void ListViewStar_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.ListViewStar.SelectedIndex >= 0)
            {
                this.TextIP.Text = Config.Stars[this.ListViewStar.SelectedIndex].Info;
            }
        }

        private void ListViewHistoryItem_DoubleClick(object sender, RoutedEventArgs e)
        {
            this.ButtonConnect_MouseLeftDown(null, null);
        }

        private void ListViewStarItem_DoubleClick(object sender, RoutedEventArgs e)
        {
            this.ButtonConnect_MouseLeftDown(null, null);
        }

        private void ListViewHistory_GotFocus(object sender, RoutedEventArgs e)
        {
            _lastFocusListView = "History";
        }

        private void ListViewStar_GotFocus(object sender, RoutedEventArgs e)
        {
            _lastFocusListView = "Star";
        }

        private ConnectionRecord GetSelectedItem()
        {
            if (_lastFocusListView == "History")
            {
                if (this.ListViewHistory.SelectedIndex < 0) return null;
                return Config.Histories[this.ListViewHistory.SelectedIndex];
            }
            else if (_lastFocusListView == "Star")
            {
                if (this.ListViewStar.SelectedIndex < 0) return null;
                return Config.Stars[this.ListViewStar.SelectedIndex];
            }
            return null;
        }


        private void ButtonStar_Click(object sender, RoutedEventArgs e)
        {
            if (_lastFocusListView == "") return;
            ConnectionRecord connectionRecord = GetSelectedItem();
            if (connectionRecord == null || connectionRecord.IsStarred) return;
            Config.Star(connectionRecord);
        }

        private void ButtonUnstar_Click(object sender, RoutedEventArgs e)
        {
            if (_lastFocusListView == "") return;
            ConnectionRecord connectionRecord = GetSelectedItem();
            if (connectionRecord == null || !connectionRecord.IsStarred) return;
            Config.UnStar(connectionRecord);
        }

        private void TextBoxTextIP_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ButtonConnect_MouseLeftDown(null, null);
            }
        }

        private void ButtonConnect_MouseLeftDown(object sender, MouseButtonEventArgs e)
        {
            TCPAddress tcpAddress = new TCPAddress();
            try
            {
                int port = Config.DefaultPort;
                string ip_str = this.TextIP.Text;
                if (ip_str.Contains(":"))
                {
                    port = int.Parse(ip_str.Split(':')[1]);
                    ip_str = ip_str.Split(':')[0];
                }
                tcpAddress.IP = IPAddress.Parse(ip_str);
                tcpAddress.Port = port;
            }
            catch (Exception)
            {
                MessageBox.Show("Invalid address syntax");
                Logger.Log("Invalid address syntax : " + this.TextIP.Text, LogLevel.Warn);
                return;
            }

            if (IsConnecting) { return; }
            IsConnecting = true;
            try
            {
                SocketClient s = new SocketClient(tcpAddress, (ex) => {
                    this.ButtonConnect.Dispatcher.BeginInvoke(new Action(() => {
                        this.ButtonConnect.Content = "Connect";
                    }));
                    MessageBox.Show(ex.Message);
                });
                Logger.Log("Start connection to " + this.TextIP.Text, LogLevel.Info);
                this.ButtonConnect.Content = "Connecting ...";
                s.AsyncConnect(() => {
                    s.Close();
                    // 线程锁应该是lock(this), 所以所有this内部成员的访问都要通过Invoke进行
                    this.ButtonConnect.Dispatcher.BeginInvoke(new Action(() => {
                        Logger.Log("Connection to " + this.TextIP.Text + " success", LogLevel.Info);
                        this.parent.ServerAddress = tcpAddress;
                        Config.InsertHistory(new ConnectionRecord
                        {
                            Info = this.TextIP.Text
                        });
                        this.parent.StartConnectionMonitor();
                        //this.parent.IpTitle.Text = "Connected IP : " + this.TextIP.Text;
                        this.parent.RedirectPage("Browser");
                        System.Threading.Thread.Sleep(100);
                        this.parent.SubPageBrowser.ResetRemoteDirectory();
                        this.parent.SubPageBrowser.ButtonRefresh_Click(null, null);
                        //this.parent.ListFiles();
                    }));
                });
            }
            catch (Exception ex)
            {
                Logger.Log("Connection to " + this.TextIP.Text + " failed. " + ex.Message, LogLevel.Info);
                MessageBox.Show(ex.Message);
            }
            finally
            {
                this.ButtonConnect.Content = "Connect";
                IsConnecting = false;
            }
        }
    }
}
