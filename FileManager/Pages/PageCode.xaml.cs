using FileManager.Windows.Configuration;
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

namespace FileManager.Pages
{
    /// <summary>
    /// PageCode.xaml 的交互逻辑
    /// </summary>
    public partial class PageCode : Page
    {
        public PageCode()
        {
            InitializeComponent();
        }

        private void Button1_Click(object sender, RoutedEventArgs e)
        {
            CertificateSetupWindow window = new CertificateSetupWindow();
            if (window.ShowDialog() == true)
            {
                return;
            }
        }

        private void Button2_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Button3_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
