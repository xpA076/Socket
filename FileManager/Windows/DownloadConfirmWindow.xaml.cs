using FileManager.Events;
using System;
using System.Collections.Generic;
using System.IO;
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
using System.Windows.Shapes;

namespace FileManager.Windows
{
    /// <summary>
    /// DownloadConfirmWindow.xaml 的交互逻辑
    /// </summary>
    public partial class DownloadConfirmWindow : Window
    {
        private const string DefaultPathDisplay = "N / A";

        private FolderBrowserDialog folderDialog = new FolderBrowserDialog();

        private static string LastPath = DefaultPathDisplay;

        public string SelectedPath { get; set; }

        public DownloadConfirmWindow()
        {
            InitializeComponent();
            // todo 选择默认路径模式
            if (true)
            {
                // 以后不是 if (true)
                this.DownloadPath.Text = LastPath;
            }

            if (this.DownloadPath.Text == DefaultPathDisplay)
            {
                this.ButtonDownload.Visibility = Visibility.Hidden;
            }
            else
            {
                SelectedPath = LastPath;
            }
        }

        private void Topbar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void WindowClose_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            CloseWindow();
        }

        private void ButtonDownload_Click(object sender, RoutedEventArgs e)
        {
            LastPath = SelectedPath;
            this.DialogResult = true;
            this.Close();
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            CloseWindow();
        }

        private void CloseWindow()
        {
            this.DialogResult = false;
            this.Close();
        }

        private void ButtonChoosePath_Click(object sender, RoutedEventArgs e)
        {
            folderDialog.SelectedPath = LastPath;
            if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                SelectedPath = folderDialog.SelectedPath;
                this.DownloadPath.Text = SelectedPath;
                this.ButtonDownload.Visibility = Visibility.Visible;
            }
        }

        private void DownloadPath_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            PathSetWindow pathSetWindow = new PathSetWindow();
            pathSetWindow.CheckPathCallback += SetLocalPath_Click;
            if (pathSetWindow.ShowDialog() == true)
            {
                SelectedPath = pathSetWindow.Path;
                this.DownloadPath.Text = SelectedPath;
                this.ButtonDownload.Visibility = Visibility.Visible;
            }
        }


        private void SetLocalPath_Click(object sender, CheckPathEventArgs e)
        {
            e.IsPathValid = Directory.Exists(e.Path);
        }
    }
}
