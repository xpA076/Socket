using SocketFileManager.SocketLib;
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

namespace SocketFileManager.Pages
{
    /// <summary>
    /// PageBrowser.xaml 的交互逻辑
    /// </summary>
    public partial class PageBrowser : Page
    {
        private MainWindow parent;
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

        }

        private List<string> remoteDirs = new List<string>();
        private string RemoteDirectory
        {
            get
            {
                string dir = "";
                for(int i = 0; i < remoteDirs.Count; ++i)
                {
                    dir += remoteDirs[i] + "\\";
                    //if (i != remoteDirs.Count - 1) { dir += "\\"; }
                }
                return dir;
            }
        }
        private SokcetFileClass[] fileClasses;

        public bool ListFiles(string path = "")
        {
            SocketClient client = new SocketClient(this.parent.ServerIP, Config.ServerPort);
            try
            {
                client.Connect();
                fileClasses = client.RequestDirectory(RemoteDirectory);
                client.Close();
            }
            catch(Exception ex)
            {
                MessageBox.Show("Requesting remote directory [" + path + "] failure : " + ex.Message);
                return false;
            }
            this.ListBoxFile.ItemsSource = fileClasses;
            this.TextRemoteDirectory.Text = RemoteDirectory;
            return true;
        }

    }
}
