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
using System.Net;
using System.Collections.ObjectModel;

using FileManager.SocketLib;
using FileManager.SocketLib.Enums;
using FileManager.Windows;
using FileManager.Models;
using FileManager.Static;



namespace FileManager.Pages
{
    /// <summary>
    /// PageConnect.xaml 的交互逻辑
    /// </summary>
    public partial class PageConnect : Page
    {
        private FileManagerMainWindow parent = null;


        private bool IsConnecting { get; set; } = false;

        private string _lastFocusListView = "";



        public PageConnect()
        {
            InitializeComponent();
            //this.ButtonConnect.PreviewMouseLeftButtonDown += new MouseButtonEventHandler(ButtonConnect_MouseLeftDown);
            if (Config.Instance.Histories.Count > 0)
            {
                this.TextBoxIP.Text = Config.Instance.Histories[0].Info;
                //this.TextBoxProxy.Text = this.TextBoxIP.Text;
            }
            
            this.ListViewHistory.ItemsSource = Config.Instance.Histories;
            this.ListViewStar.ItemsSource = Config.Instance.Stars;
            
        }


        public PageConnect(FileManagerMainWindow parent) : this()
        {
            this.parent = parent;
        }



        private void ListViewHistory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.ListViewHistory.SelectedIndex >= 0)
            {
                this.TextBoxIP.Text = Config.Instance.Histories[this.ListViewHistory.SelectedIndex].Info;
            }
            
        }

        private void ListViewStar_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.ListViewStar.SelectedIndex >= 0)
            {
                this.TextBoxIP.Text = Config.Instance.Stars[this.ListViewStar.SelectedIndex].Info;
            }
        }

        private void ListViewHistoryItem_DoubleClick(object sender, RoutedEventArgs e)
        {
            this.ButtonConnect_Click(null, null);
        }

        private void ListViewStarItem_DoubleClick(object sender, RoutedEventArgs e)
        {
            this.ButtonConnect_Click(null, null);
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
                return Config.Instance.Histories[this.ListViewHistory.SelectedIndex];
            }
            else if (_lastFocusListView == "Star")
            {
                if (this.ListViewStar.SelectedIndex < 0) return null;
                return Config.Instance.Stars[this.ListViewStar.SelectedIndex];
            }
            return null;
        }


        private void ButtonStar_Click(object sender, RoutedEventArgs e)
        {
            if (_lastFocusListView == "") return;
            ConnectionRecord connectionRecord = GetSelectedItem();
            if (connectionRecord == null || connectionRecord.IsStarred) return;
            Config.Instance.Star(connectionRecord);
        }

        private void ButtonUnstar_Click(object sender, RoutedEventArgs e)
        {
            if (_lastFocusListView == "") return;
            ConnectionRecord connectionRecord = GetSelectedItem();
            if (connectionRecord == null || !connectionRecord.IsStarred) return;
            Config.Instance.UnStar(connectionRecord);
        }

        private void TextBoxIP_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ButtonConnect_Click(null, null);
            }
        }

        private void TextBoxProxy_KeyDown(object sender, KeyEventArgs e)
        {
            TextBoxIP_KeyDown(sender, e);
        }



        private void ButtonConnect_Click(object sender, RoutedEventArgs e)
        {
            if (IsConnecting) { return; }
            try
            {
                SocketFactory.Instance.CurrentRoute = ConnectionRoute.FromString(this.TextBoxIP.Text, this.TextBoxProxy.Text, Config.Instance.DefaultServerPort, Config.Instance.DefaultProxyPort);
            }
            catch (Exception)
            {
                SocketFactory.Instance.CurrentRoute = null;
                MessageBox.Show("Invalid address syntax");
                LoggerStatic.Log("Invalid address syntax : " + this.TextBoxIP.Text, LogLevel.Warn);
                return;
            }
            try
            {
                IsConnecting = true;
                LoggerStatic.Log("Start connection to " + this.TextBoxIP.Text, LogLevel.Info);
                this.ButtonConnect.Content = "Connecting ...";
                //SocketIdentity identity = SocketFactory.AsyncConnectForIdentity(AsyncConnect_OnSuccess, AsyncConnect_OnException);
                SocketFactory.Instance.AsyncConnectForIdentity(AsyncConnect_OnSuccess, AsyncConnect_OnException);
            }
            catch (Exception ex)
            {
                /// AsyncConnect 的异常在上面的 SocketAsyncExceptionCallback 中处理
                /// 这里的代码应该不会执行
                SocketFactory.Instance.CurrentRoute = null;
                LoggerStatic.Log("[Not expected exception] Connection to " + this.TextBoxIP.Text + " failed. " + ex.Message, LogLevel.Info);
                MessageBox.Show(ex.Message);
                IsConnecting = false;
            }
            /// 这里如果写 finally 的话, 会执行于异步代码 AsyncConnect 之前
            /// 所以不应在这里用 finally 处理, 后续处理应该写进 AysncConnect 的代理方法内
        }

        private void AsyncConnect_OnSuccess(object sender, EventArgs e)
        {
            /// 因为异步执行AsyncConnect在新线程, 所以所有this的UI更新都要通过BeginInvoke进行
            this.ButtonConnect.Dispatcher.BeginInvoke(new Action(() =>
            {
                this.ButtonConnect.Content = "Connect";
                LoggerStatic.Log("Connection to " + this.TextBoxIP.Text + " success", LogLevel.Info);
                Config.Instance.InsertHistory(new ConnectionRecord
                {
                    Info = this.TextBoxIP.Text
                });
                //this.parent.StartConnectionMonitor();
                this.parent.RedirectPage("Browser");
                System.Threading.Thread.Sleep(100);
                this.parent.SubPageBrowser.ResetRemoteDirectory();
                this.parent.SubPageBrowser.ButtonRefresh_Click(null, null);
            }));
            IsConnecting = false;
        }


        private void AsyncConnect_OnException(object sender, SocketAsyncExceptionEventArgs e)
        {
            SocketFactory.Instance.CurrentRoute = null;
            this.ButtonConnect.Dispatcher.BeginInvoke(new Action(() =>
            {
                this.ButtonConnect.Content = "Connect";
            }));
            MessageBox.Show("Build connection failed : " + e.ExceptionMessage);
            IsConnecting = false;
        }





        private void TextBoxIP_LostFocus(object sender, RoutedEventArgs e)
        {
            // todo
            //int a =1;
        }
    }
}
