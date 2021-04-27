using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;

using FileManager.Models;
using FileManager.ViewModels;
using FileManager.Static;
using FileManager.Windows;
using SocketLib;
using SocketLib.Enums;

namespace FileManager.Pages
{
    /// <summary>
    /// PageBrowser.xaml 的交互逻辑
    /// </summary>
    public partial class PageBrowser : Page
    {
        private MainWindow parent;
        private OpenFileDialog fileDialog = new OpenFileDialog();
        //private UploadSelectWindow uploadSelectWindow = new UploadSelectWindow();
        private List<SocketFileInfo> fileClasses;

        private List<string> RemoteDirArray = new List<string>();

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


        private string RemoteDirectory
        {
            get
            {
                string dir = "";
                for (int i = 0; i < RemoteDirArray.Count; ++i)
                {
                    dir += RemoteDirArray[i];
                    //if (i != RemoteDirArray.Count - 1) { dir += "\\"; }
                    dir += "\\";
                }
                return dir;
            }
        }

        private readonly BrowserIPViewModel browserIPView = new BrowserIPViewModel();


        public PageBrowser(MainWindow parent)
        {
            this.parent = parent;
            InitializeComponent();
            // buttons
            /*
            this.ButtonBack.PreviewMouseLeftButtonDown += new MouseButtonEventHandler(ButtonBack_Click);
            this.ButtonOpen.PreviewMouseLeftButtonDown += new MouseButtonEventHandler(ButtonOpen_Click);
            this.ButtonDownload.PreviewMouseLeftButtonDown += new MouseButtonEventHandler(ButtonDownload_Click);
            this.ButtonUpload.PreviewMouseLeftButtonDown += new MouseButtonEventHandler(ButtonUpload_Click);
            this.ButtonRefresh.PreviewMouseLeftButtonDown += new MouseButtonEventHandler(ButtonRefresh_Click);
            */
            // dialog
            this.TextBlockConnectedIP.DataContext = browserIPView;
            fileDialog.Multiselect = true;
        }

        public void SetConnectedIPText(TCPAddress address)
        {
            this.browserIPView.ServerAddress = address;
        }

        public void ResetRemoteDirectory()
        {
            this.RemoteDirArray = new List<string>();
        }


        #region Button actions

        public async void ButtonRefresh_Click(object sender, RoutedEventArgs e)
        {
            if (this.parent.ServerAddress == null) return;
            this.ButtonRefresh.Visibility = Visibility.Hidden;
            _ = await ListFiles();
            this.ButtonRefresh.Visibility = Visibility.Visible;
        }


        private async void ButtonBack_Click(object sender, RoutedEventArgs e)
        {
            if (RemoteDirArray.Count > 0)
            {
                this.ButtonBack.Content = "Back ...";
                string temp = RemoteDirArray.Last();
                RemoteDirArray.RemoveAt(RemoteDirArray.Count - 1);
                bool result = await ListFiles();
                this.ButtonBack.Content = "Back";
                if (!result) { RemoteDirArray.Add(temp); }
            }
        }


        private async void ButtonOpen_Click(object sender, RoutedEventArgs e)
        {
            if (this.ListViewFile.SelectedIndex < 0) return;
            if (fileClasses[this.ListViewFile.SelectedIndex].IsDirectory)
            {
                this.ButtonOpen.Content = "Open ...";
                RemoteDirArray.Add(fileClasses[this.ListViewFile.SelectedIndex].Name);
                bool result = await ListFiles();
                this.ButtonOpen.Content = "Open";
                if (!result) { RemoteDirArray.RemoveAt(RemoteDirArray.Count - 1); }
            }
        }


        private void ButtonDownload_Click(object sender, RoutedEventArgs e)
        {
            if (this.ListViewFile.SelectedIndex < 0) { return; }
            List<FileTask> fileTasks = new List<FileTask>();
            //var a = this.ListViewFile.SelectedItems;
            //var selected = fileClasses[this.ListViewFile.SelectedIndex];
            foreach (SocketFileInfo selected in this.ListViewFile.SelectedItems)
            {
                FileTask task = new FileTask
                {
                    TcpAddress = SocketFactory.TcpAddress.Copy(),
                    IsDirectory = selected.IsDirectory,
                    Type = TransferType.Download,
                    RemotePath = RemoteDirectory + selected.Name,
                    Length = selected.Length,
                };
                if (task.IsDirectory)
                {
                    task.Length = FileTaskManager.GetDirectoryTaskLength(task);
                }
                fileTasks.Add(task);
            }

            DownloadConfirm(fileTasks);
        }

        public void DownloadConfirm(List<FileTask> fileTasks)
        {
            ConfirmWindow downloadConfirmWindow = new ConfirmWindow();
            downloadConfirmWindow.ListViewTask.ItemsSource = fileTasks; // to edit
            if (downloadConfirmWindow.ShowDialog() != true) { return; }

            string localPath = downloadConfirmWindow.SelectedPath;
            foreach (FileTask ft in fileTasks)
            {
                ft.LocalPath = System.IO.Path.Combine(localPath, ft.Name);
            }
            this.parent.SubPageTransfer.AddTasks(fileTasks);
            this.parent.RedirectPage("Transfer");
        }


        private void ButtonUpload_Click(object sender, RoutedEventArgs e)
        {
            string remoteDir = this.RemoteDirectory;
            if (remoteDir == "") { return; }
            UploadSelectWindow uploadSelectWindow = new UploadSelectWindow();
            uploadSelectWindow.DisplayPath = remoteDir;
            if (uploadSelectWindow.ShowDialog() != true) { return; }
            if (uploadSelectWindow.UploadChoosen == FileManager.Windows.UploadChoose.Files)
            {
                foreach (string localPath in uploadSelectWindow.UploadPathList)
                {
                    int idx = localPath.LastIndexOf("\\");
                    string name = localPath.Substring(idx + 1, localPath.Length - (idx + 1));
                    this.parent.SubPageTransfer.AddTask(new FileTask
                    {
                        IsDirectory = false,
                        Type = TransferType.Upload,
                        RemotePath = remoteDir + name,
                        LocalPath = localPath,
                        Length = new FileInfo(localPath).Length
                    });
                }
            }
            else if (uploadSelectWindow.UploadChoosen == FileManager.Windows.UploadChoose.Folder)
            {
                string localPath = uploadSelectWindow.UploadPathList[0];
                int idx = localPath.LastIndexOf("\\");
                string name = localPath.Substring(idx + 1, localPath.Length - (idx + 1));
                this.parent.SubPageTransfer.AddTask(new FileTask
                {
                    IsDirectory = true,
                    Type = TransferType.Upload,
                    RemotePath = remoteDir + name,
                    LocalPath = localPath,
                    Length = 0,
                });
            }
            this.parent.RedirectPage("Transfer");
        }

        public void ButtonCreate_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.MessageBox.Show("Create not implemented");
            //this.parent.StopConnectionMonitor();
        }

        public void ButtonDelete_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.MessageBox.Show("Delete not implemented");
        }

        public void ButtonNewConnection_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.MessageBox.Show("New connection not implemented");
        }

        #endregion


        public void ListViewFileItem_DoubleClick(object sender, RoutedEventArgs e)
        {
            if (fileClasses[this.ListViewFile.SelectedIndex].IsDirectory)
            {
                ButtonOpen_Click(null, null);
            }
            else
            {
                ButtonDownload_Click(null, null);
            }
        }


        /// <summary>
        /// 获取 server 文件列表
        /// </summary>
        /// <returns>请求是否成功 bool 值</returns>
        private Task<bool> ListFiles()
        {
            return Task.Run(() => {
                try
                {

                    // 向 server 请求文件列表
                    Logger.Log("Requesting directory : " + RemoteDirectory, LogLevel.Info);
                    SocketClient client = SocketFactory.GenerateConnectedSocketClient(1);
                    this.fileClasses = client.RequestDirectory(RemoteDirectory);
                    client.Close();
                    this.Dispatcher.Invoke(() => {
                        //this.ListBoxFile.ItemsSource = this.fileClasses;
                        this.ListViewFile.ItemsSource = this.fileClasses;
                        this.TextRemoteDirectory.Text = RemoteDirectory;
                    });
                    return true;
                }
                catch (Exception ex)
                {
                    string msg = "Requesting remote directory \"" + RemoteDirectory + "\" failure : " + ex.Message;
                    Logger.Log(msg, LogLevel.Warn);
                    System.Windows.Forms.MessageBox.Show(msg);
                    return false;
                }
            });
        }

        private void ListViewFile_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            System.Windows.Controls.ListView listView = sender as System.Windows.Controls.ListView;
            GridView gView = listView.View as GridView;

            var workingWidth = listView.ActualWidth - SystemParameters.VerticalScrollBarWidth;
            //workingWidth = listView.ActualWidth;
            gView.Columns[0].Width = 40;
            gView.Columns[1].Width = 300;
            gView.Columns[2].Width = 70;
            gView.Columns[3].Width = workingWidth - 410;
        }
    }
}
