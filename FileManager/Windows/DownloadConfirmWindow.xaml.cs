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
using System.Windows.Shapes;

namespace FileManager.Windows
{
    /// <summary>
    /// DownloadConfirmWindow.xaml 的交互逻辑
    /// </summary>
    public partial class DownloadConfirmWindow : Window
    {
        private FolderBrowserDialog folderDialog = new FolderBrowserDialog();

        private static string DefaultPath = "";

        public string SelectedPath { get; set; }

        public DownloadConfirmWindow()
        {
            InitializeComponent();
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
            folderDialog.SelectedPath = DefaultPath;
            if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                SelectedPath = folderDialog.SelectedPath;
                this.DialogResult = true;
                DefaultPath = SelectedPath;
            }
            else
            {
                this.DialogResult = false;
            }
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
    }
}
