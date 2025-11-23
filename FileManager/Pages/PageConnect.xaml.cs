using FileManager.Events;
using FileManager.Models;
using FileManager.Models.Log;
using FileManager.Models.Serializable.HeartBeat;
using FileManager.Models.SocketLib.Enums;
using FileManager.Models.SocketLib.Models;
using FileManager.Models.SocketLib.SocketClient;
using FileManager.Models.SocketLib.SocketIO;
using FileManager.Services.Config;
using FileManager.Static;
using FileManager.Windows;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace FileManager.Pages
{
    /// <summary>
    /// PageConnect.xaml 的交互逻辑
    /// </summary>
    public partial class PageConnect : Page
    {

        private LogService logService = Program.Provider.GetRequiredService<LogService>();
        private ConfigService configService = Program.Provider.GetRequiredService<ConfigService>();
        private ClientConfigStorage clientConfig = Program.Provider.GetRequiredService<ClientConfigStorage>();
        private readonly SocketClientDispatcher socketClientDispatcher = Program.Provider.GetRequiredService<SocketClientDispatcher>();


        private FileManagerMainWindow parent = null;


        private bool _isConnecting { get; set; } = false;

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



        private async void ButtonConnect_Click(object sender, RoutedEventArgs e)
        {
            if (_isConnecting) { return; }
            try
            {
                var address = TCPAddress.Build(this.TextBoxIP.Text, configService.DefaultServerPort);
                socketClientDispatcher.SetHostAddress(address);
                
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
                /// Before connect
                _isConnecting = true;
                logService.Log("Start connection to " + this.TextBoxIP.Text, LogLevel.Info);
                this.ButtonConnect.Content = "Connecting ...";

                /// Connect
                socketClientDispatcher.Initialize();
                var resp = await socketClientDispatcher.RequestAsync(HeartBeatRequest.Single);

                /// After connect
                logService.Log("Connection to " + this.TextBoxIP.Text + " success", LogLevel.Info);
                clientConfig.InsertHistory(new ConnectionRecord
                {
                    Info = this.TextBoxIP.Text
                });
                this.parent.RedirectPage("Browser");
                this.parent.SubPageBrowser.ResetRemoteDirectory();
                this.parent.SubPageBrowser.ButtonRefresh_Click(null, null);
            }
            catch (Exception ex)
            {
                socketClientDispatcher.ShutDown();
                logService.Log("[Not expected exception] Connection to " + this.TextBoxIP.Text + " failed. " + ex.Message, LogLevel.Info);
                System.Windows.MessageBox.Show(ex.Message);
                _isConnecting = false;
            }
            finally
            {
                await this.ButtonConnect.Dispatcher.BeginInvoke(new Action(() =>
                {
                    this.ButtonConnect.Content = "Connect";
                }));
            }
        }

        private void TextBoxIP_LostFocus(object sender, RoutedEventArgs e)
        {
            // todo
            //int a =1;
        }
    }
}
