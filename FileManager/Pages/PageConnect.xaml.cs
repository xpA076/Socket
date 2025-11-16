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

using FileManager.Windows;
using FileManager.Models;
using FileManager.Static;
using FileManager.Models.SocketLib.SocketIO;
using FileManager.Events;
using FileManager.Models.SocketLib.Models;
using FileManager.Models.SocketLib.Enums;
using FileManager.Models.Config;
using Microsoft.Extensions.DependencyInjection;
using FileManager.Models.Log;

namespace FileManager.Pages
{
    /// <summary>
    /// PageConnect.xaml 的交互逻辑
    /// </summary>
    public partial class PageConnect : Page
    {

        private LogService logService = Program.Provider.GetService<LogService>();
        private ConfigService configService = Program.Provider.GetService<ConfigService>();
        private ClientConfigStorage clientConfig = Program.Provider.GetService<ClientConfigStorage>();

        private FileManagerMainWindow parent = null;


        private bool IsConnecting { get; set; } = false;

        private string _lastFocusListView = "";



        public PageConnect()
        {
            InitializeComponent();
            //this.ButtonConnect.PreviewMouseLeftButtonDown += new MouseButtonEventHandler(ButtonConnect_MouseLeftDown);
            if (clientConfig.Histories.Count > 0)
            {
                this.TextBoxIP.Text = clientConfig.Histories[0].Info;
                //this.TextBoxProxy.Text = this.TextBoxIP.Text;
            }
            
            this.ListViewHistory.ItemsSource = clientConfig.Histories;
            this.ListViewStar.ItemsSource = clientConfig.Stars;
            
        }


        public PageConnect(FileManagerMainWindow parent) : this()
        {
            this.parent = parent;
        }



        private void ListViewHistory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.ListViewHistory.SelectedIndex >= 0)
            {
                this.TextBoxIP.Text = clientConfig.Histories[this.ListViewHistory.SelectedIndex].Info;
            }
            
        }

        private void ListViewStar_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.ListViewStar.SelectedIndex >= 0)
            {
                this.TextBoxIP.Text = clientConfig.Stars[this.ListViewStar.SelectedIndex].Info;
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
                return clientConfig.Histories[this.ListViewHistory.SelectedIndex];
            }
            else if (_lastFocusListView == "Star")
            {
                if (this.ListViewStar.SelectedIndex < 0) return null;
                return clientConfig.Stars[this.ListViewStar.SelectedIndex];
            }
            return null;
        }


        private void ButtonStar_Click(object sender, RoutedEventArgs e)
        {
            if (_lastFocusListView == "") return;
            ConnectionRecord connectionRecord = GetSelectedItem();
            if (connectionRecord == null || connectionRecord.IsStarred) return;
            clientConfig.Star(connectionRecord);
        }

        private void ButtonUnstar_Click(object sender, RoutedEventArgs e)
        {
            if (_lastFocusListView == "") return;
            ConnectionRecord connectionRecord = GetSelectedItem();
            if (connectionRecord == null || !connectionRecord.IsStarred) return;
            clientConfig.UnStar(connectionRecord);
        }

        private void TextBoxIP_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ButtonConnect_Click(null, null);
            }
        }

        private void TextBoxProxy_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            TextBoxIP_KeyDown(sender, e);
        }



        private void ButtonConnect_Click(object sender, RoutedEventArgs e)
        {
            if (IsConnecting) { return; }
            try
            {
                SocketFactory.Instance.CurrentRoute = ConnectionRoute.FromString(this.TextBoxIP.Text, this.TextBoxProxy.Text, configService.DefaultServerPort, configService.DefaultProxyPort);
            }
            catch (Exception)
            {
                SocketFactory.Instance.CurrentRoute = null;
                System.Windows.MessageBox.Show("Invalid address syntax");
                logService.Log("Invalid address syntax : " + this.TextBoxIP.Text, LogLevel.Warn);
                return;
            }
            try
            {
                IsConnecting = true;
                logService.Log("Start connection to " + this.TextBoxIP.Text, LogLevel.Info);
                this.ButtonConnect.Content = "Connecting ...";
                //SocketIdentity identity = SocketFactory.AsyncConnectForIdentity(AsyncConnect_OnSuccess, AsyncConnect_OnException);
                SocketFactory.Instance.AsyncConnectForIdentity(AsyncConnect_OnSuccess, AsyncConnect_OnException);
            }
            catch (Exception ex)
            {
                /// AsyncConnect 的异常在上面的 SocketAsyncExceptionCallback 中处理
                /// 这里的代码应该不会执行
                SocketFactory.Instance.CurrentRoute = null;
                logService.Log("[Not expected exception] Connection to " + this.TextBoxIP.Text + " failed. " + ex.Message, LogLevel.Info);
                System.Windows.MessageBox.Show(ex.Message);
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
                logService.Log("Connection to " + this.TextBoxIP.Text + " success", LogLevel.Info);
                clientConfig.InsertHistory(new ConnectionRecord
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
            System.Windows.MessageBox.Show("Build connection failed : " + e.ExceptionMessage);
            IsConnecting = false;
        }





        private void TextBoxIP_LostFocus(object sender, RoutedEventArgs e)
        {
            // todo
            //int a =1;
        }
    }
}
