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
        }

        private List<string> remoteDirs = new List<string>();
        private string RemoteDirectory
        {
            get
            {
                return "";
            }
        }
        private SokcetFileClass[] fileClasses;

        public void ListFiles()
        {
            SocketClient client = new SocketClient(this.parent.ServerIP, Config.ServerPort);
            try
            {
                client.Connect();
                fileClasses = client.RequestDirectory(RemoteDirectory);
            }
            catch(Exception ex)
            {
                MessageBox.Show("Requesting remote directory failure : " + ex.Message);
                return;
            }
            this.ListBoxFile.ItemsSource = fileClasses;
        }

    }
}
