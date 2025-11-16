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

namespace FileManager.Windows.Configuration
{
    /// <summary>
    /// CertificateSetupWindow.xaml 的交互逻辑
    /// </summary>
    public partial class CertificateSetupWindow : Window
    {
        private FolderBrowserDialog folderDialog = new FolderBrowserDialog();

        private OpenFileDialog fileDialog = new OpenFileDialog();

        public CertificateSetupWindow()
        {
            InitializeComponent();
            fileDialog.Multiselect = false;
        }

        private void ButtonAddTrustedServer_Click(object sender, RoutedEventArgs e)
        {
            if (fileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string filePath = fileDialog.FileName;
                byte[] bytes = File.ReadAllBytes(filePath);
            }
        }

        private void ButtonAddTrustedClient_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ButtonRemove_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ButtonSetServerCertificate_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ButtonSetClientCertificate_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ButtonGenerateCertificate_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ButtonRefresh_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
