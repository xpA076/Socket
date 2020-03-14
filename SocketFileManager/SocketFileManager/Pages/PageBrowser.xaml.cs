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

        private void ButtonRefresh_Click(object sender, MouseButtonEventArgs e)
        {
            ListFiles();
        }


        private void ButtonBack_Click(object sender, MouseButtonEventArgs e)
        {
            if (remoteDirs.Count > 0)
            {
                string temp = remoteDirs.Last();
                remoteDirs.RemoveAt(remoteDirs.Count - 1);
                if (!ListFiles()){ remoteDirs.Add(temp); }
            }
        }

        private void ButtonOpen_Click(object sender, MouseButtonEventArgs e)
        {
            if (fileClasses[this.ListBoxFile.SelectedIndex].IsDirectory)
            {
                remoteDirs.Add(fileClasses[this.ListBoxFile.SelectedIndex].Name);
                if (!ListFiles()) { remoteDirs.RemoveAt(remoteDirs.Count - 1); }
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
        /// 应该能判断请求是否成功的，现在只返回true
        /// </summary>
        /// <returns></returns>
        public bool ListFiles()
        {
            fileClasses = this.parent.RequestDirectory(RemoteDirectory);
            this.ListBoxFile.ItemsSource = fileClasses;
            this.TextRemoteDirectory.Text = RemoteDirectory +
                (string.IsNullOrEmpty(RemoteDirectory) ? "" : "\\");
            return true;
        }

    }
}
