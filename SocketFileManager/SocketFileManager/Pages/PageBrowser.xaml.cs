using SocketFileManager.SocketLib;
using SocketFileManager.Models;

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

namespace SocketFileManager.Pages
{
    /// <summary>
    /// PageBrowser.xaml 的交互逻辑
    /// </summary>
    public partial class PageBrowser : Page
    {
        private MainWindow parent;
        private FolderBrowserDialog dialog = new FolderBrowserDialog();
        private SokcetFileClass[] fileClasses;

        public PageBrowser(MainWindow parent)
        {
            this.parent = parent;
            InitializeComponent();
            // buttons
            this.ButtonBack.PreviewMouseLeftButtonDown += new MouseButtonEventHandler(ButtonBack_Click);
            this.ButtonOpen.PreviewMouseLeftButtonDown += new MouseButtonEventHandler(ButtonOpen_Click);
            this.ButtonDownload.PreviewMouseLeftButtonDown += new MouseButtonEventHandler(ButtonDownload_Click);
            this.ButtonRefresh.PreviewMouseLeftButtonDown += new MouseButtonEventHandler(ButtonRefresh_Click);
        }

        public async void ButtonRefresh_Click(object sender, MouseButtonEventArgs e)
        {
            this.ButtonRefresh.Visibility = Visibility.Hidden;
            bool result = await ListFilesAsync();
            this.ButtonRefresh.Visibility = Visibility.Visible;
        }


        private async void ButtonBack_Click(object sender, MouseButtonEventArgs e)
        {
            if (remoteDirs.Count > 0)
            {
                this.ButtonBack.Content = "Back ...";
                string temp = remoteDirs.Last();
                remoteDirs.RemoveAt(remoteDirs.Count - 1);
                bool result = await ListFilesAsync();
                this.ButtonBack.Content = "Back";
                if (!result){ remoteDirs.Add(temp); }
            }
        }

        private async void ButtonOpen_Click(object sender, MouseButtonEventArgs e)
        {
            if (fileClasses[this.ListBoxFile.SelectedIndex].IsDirectory)
            {
                this.ButtonOpen.Content = "Open ...";
                remoteDirs.Add(fileClasses[this.ListBoxFile.SelectedIndex].Name);
                bool result = await ListFilesAsync();
                this.ButtonOpen.Content = "Open";
                if (!result) { remoteDirs.RemoveAt(remoteDirs.Count - 1); }
            }
        }

        private void ButtonDownload_Click(object sender, MouseButtonEventArgs e)
        {
            if (this.ListBoxFile.SelectedIndex < 0) { return; }
            bool isDir = fileClasses[this.ListBoxFile.SelectedIndex].IsDirectory;
            string name = fileClasses[this.ListBoxFile.SelectedIndex].Name;

            DialogResult result = System.Windows.Forms.MessageBox.Show(
                "Are you sure to download " + (isDir ? "folder" : "file") + " : " + name + " ?",
                "Download", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
            if(result == DialogResult.No) { return; }

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                string localPath = dialog.SelectedPath;
                //System.Windows.Forms.MessageBox.Show(localPath);
                this.parent.AddDownloadTask(new FileTask
                {
                    IsDirectory = isDir,
                    Type = "download",
                    RemotePath = RemoteDirectory + "\\" + name,
                    LocalPath = localPath + "\\" + name,
                    Length = fileClasses[this.ListBoxFile.SelectedIndex].Length,
                });
                this.parent.RedirectPage("Download");
            }
        }

        
        private List<string> remoteDirs = new List<string>();
        private string RemoteDirectory
        {
            get
            {
                string dir = "";
                for(int i = 0; i < remoteDirs.Count; ++i)
                {
                    dir += remoteDirs[i];
                    if (i != remoteDirs.Count - 1) { dir += "\\"; }
                }
                return dir;
            }
        }
        

        /// <summary>
        /// 获取 server 文件列表
        /// </summary>
        /// <returns>请求是否成功 bool 值</returns>
        private Task<bool> ListFilesAsync()
        {
            return Task.Run(()=> {
                try
                {
                    fileClasses = this.parent.RequestDirectory(RemoteDirectory);
                    this.Dispatcher.Invoke(() => {
                        this.ListBoxFile.ItemsSource = fileClasses;
                        this.TextRemoteDirectory.Text = RemoteDirectory +
                            (string.IsNullOrEmpty(RemoteDirectory) ? "" : "\\");
                    });
                    return true;
                }
                catch (Exception ex)
                {
                    System.Windows.Forms.MessageBox.Show("Requesting remote directory [" +
                        RemoteDirectory + "] failure : " + ex.Message);
                    return false;
                }
            });
        }

    }
}
