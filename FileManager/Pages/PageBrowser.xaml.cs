﻿using System;
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

using FileManager.Events;
using FileManager.Models;
using FileManager.ViewModels;
using FileManager.Static;
using FileManager.Windows;
using FileManager.SocketLib;
using FileManager.SocketLib.Enums;
using FileManager.Exceptions;

namespace FileManager.Pages
{
    /// <summary>
    /// PageBrowser.xaml 的交互逻辑
    /// </summary>
    public partial class PageBrowser : Page
    {
        private FileManagerMainWindow MainWindow { get; set; }
        private OpenFileDialog fileDialog = new OpenFileDialog();
        //private UploadSelectWindow uploadSelectWindow = new UploadSelectWindow();
        private List<SocketFileInfo> fileClasses;

        private List<string> RemoteDirArray = new List<string>();


        private ConnectionRoute CurrentRoute
        {
            get
            {
                return SocketFactory.CurrentRoute.Copy();
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


        public PageBrowser(FileManagerMainWindow parent)
        {
            this.MainWindow = parent;
            InitializeComponent();
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

        private void ButtonSetPath_Click(object sender, RoutedEventArgs e)
        {
            PathSetWindow pathSetWindow = new PathSetWindow();
            pathSetWindow.CheckPathCallback += SetRemotePath_Click;

            if (pathSetWindow.ShowDialog() != true)
            {
                return;
            }
            ManuallySetRemotePath(pathSetWindow.Path);
            ButtonRefresh_Click(null, null);
        }

        /// <summary>
        /// 对于手动设置(从PathSetWindow中设置)的RemotePath写入RemoteDirArray
        /// (以后用作路径翻译的时候可能用到, 就单写一个函数)
        /// </summary>
        /// <param name="path"></param>
        private void ManuallySetRemotePath(string path)
        {
            string[] ss = path.Split('\\');
            RemoteDirArray.Clear();
            for (int i = 0; i < ss.Length; ++i)
            {
                if (!string.IsNullOrEmpty(ss[i]))
                {
                    RemoteDirArray.Add(ss[i]);
                }
            }
        }

        /// <summary>
        /// 子窗体 PathSetWindow 中 Set按钮回调函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SetRemotePath_Click(object sender, CheckPathEventArgs e)
        {
            try
            {
                string s = e.Path;
                var resp = SocketFactory.Request(new HB32Header { Flag = SocketPacketFlag.DirectoryCheck }, Encoding.UTF8.GetBytes(e.Path));
                if (resp.Header.Flag != SocketPacketFlag.DirectoryResponse)
                {
                    throw new SocketFlagException();
                }
                e.IsPathValid = true;
            }
            catch (Exception)
            {
                e.IsPathValid = false;
            }
        }

        public async void ButtonRefresh_Click(object sender, RoutedEventArgs e)
        {
            if (!this.MainWindow.IsConnected) return;
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
            if (Config.UseLegacyFileInfo)
            {
                List<FileTask> fileTasks = new List<FileTask>();
                foreach (SocketFileInfo selected in this.ListViewFile.SelectedItems)
                {
                    FileTask task = new FileTask
                    {
                        Route = SocketFactory.CurrentRoute.Copy(),
                        IsDirectory = selected.IsDirectory,
                        Type = TransferType.Download,
                        RemotePath = RemoteDirectory + selected.Name,
                        Length = selected.Length,
                    };
                    if (task.IsDirectory)
                    {
                        task.Length = FileTasksManager.GetDownloadDirectoryTaskLength(task);
                    }
                    fileTasks.Add(task);
                }
                DownloadConfirmLegacy(fileTasks);
            }
            else
            {
                TransferRootInfo rootInfo = new TransferRootInfo();
                rootInfo.Route = CurrentRoute.Copy();
                rootInfo.Rule = new FilterRule();
                rootInfo.Type = TransferType.Download;
                rootInfo.RemoteDirectory = RemoteDirectory;
                List<SocketFileInfo> selectedInfos = new List<SocketFileInfo>();
                foreach (SocketFileInfo selected in this.ListViewFile.SelectedItems)
                {
                    selectedInfos.Add(selected.Copy());
                }
                rootInfo.BuildChildrenFrom(selectedInfos);
                DownloadConfirm(rootInfo);
            }
        }

        private void DownloadConfirm(TransferRootInfo rootInfo)
        {
            DownloadConfirmWindow downloadConfirmWindow = new DownloadConfirmWindow();
            TransferRootInfoQuerier querier = new TransferRootInfoQuerier(rootInfo);
            downloadConfirmWindow.ListViewTask.ItemsSource = querier.LinkDownloadConfirmViewModels();
            querier.StartQuery();
            if (downloadConfirmWindow.ShowDialog() == true) 
            {
                querier.UnlinkDownloadConfirmViewModels();
                rootInfo.LocalDirectory = downloadConfirmWindow.SelectedPath;
                MainWindow.SubPageTransfer.AddTransferTask(rootInfo);
                MainWindow.RedirectPage("Transfer");
            }
            else
            {
                querier.StopQuery();
            }
        }

        private void DownloadConfirmLegacy(List<FileTask> fileTasks)
        {
            DownloadConfirmWindow downloadConfirmWindow = new DownloadConfirmWindow();
            downloadConfirmWindow.ListViewTask.ItemsSource = fileTasks; // to edit
            if (downloadConfirmWindow.ShowDialog() != true) { return; }

            string localPath = downloadConfirmWindow.SelectedPath;
            foreach (FileTask ft in fileTasks)
            {
                ft.LocalPath = System.IO.Path.Combine(localPath, ft.Name);
            }
            this.MainWindow.SubPageTransfer.AddTasks(fileTasks);
            this.MainWindow.RedirectPage("TransferLegacy");
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
                    this.MainWindow.SubPageTransfer.AddTask(new FileTask
                    {
                        Route = SocketFactory.CurrentRoute.Copy(),
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
                this.MainWindow.SubPageTransfer.AddTask(new FileTask
                {
                    Route = SocketFactory.CurrentRoute.Copy(),
                    IsDirectory = true,
                    Type = TransferType.Upload,
                    RemotePath = remoteDir + name,
                    LocalPath = localPath,
                    Length = 0,
                });
            }
            this.MainWindow.RedirectPage("Transfer");
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
                    Logger.Log("Requesting directory : " + RemoteDirectory, LogLevel.Info);
                    HB32Response resp = SocketFactory.Request(SocketPacketFlag.DirectoryRequest, Encoding.UTF8.GetBytes(RemoteDirectory));
                    this.fileClasses = SocketFileInfo.BytesToList(resp.Bytes);
                    this.Dispatcher.Invoke(() => {
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
