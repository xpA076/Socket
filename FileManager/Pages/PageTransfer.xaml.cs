using FileManager.Models;
using FileManager.Models.TransferLib;
using FileManager.ViewModels;
using FileManager.Events;
using FileManager.ViewModels.PageTransfer;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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
using FileManager.Models.TransferLib.Info;
using FileManager.Models.TransferLib.Services;
using MessageBox = System.Windows.MessageBox;

namespace FileManager.Pages
{
    /// <summary>
    /// PageDownload.xaml 的交互逻辑
    /// </summary>
    public partial class PageTransfer : Page
    {

        private FileManagerMainWindow parent;

        public PageTransfer(FileManagerMainWindow parent)
        {
            this.parent = parent;
            InitializeComponent();
            PageViewModel = new PageTransferViewModel();
            PageViewModel.InfoRoots = TransferManager.InfoRoots;
            this.ListViewTransfer.ItemsSource = PageViewModel.ListViewItems;
            this.DataContext = PageViewModel;
            this.TransferManager.ViewModel = PageViewModel;
        }
        
               
        private PageTransferViewModel PageViewModel { get; set; } = new PageTransferViewModel();

        private TransferManager TransferManager = new TransferManager();

        

        

        public bool IsTransfering
        {
            get
            {
                return TransferManager.IsTransfering;
            }
        }


        private void GridCurrentProgress_Click(object sender, MouseButtonEventArgs e)
        {
            PageViewModel.ChangeCurrentProgressDisplay();
        }


        private void GridTotalProgress_Click(object sender, MouseButtonEventArgs e)
        {
            PageViewModel.ChangeTotalProgressDisplay();
        }


        public void ButtonPause_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Not implemented");
        }

        private void ButtonResume_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Not implemented");
        }


        public void AddTransferTask(TransferInfoRoot rootInfo)
        {
            this.TransferManager.AddTransferTask(rootInfo);
        }

        private void ListViewTransferItem_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            ListViewTransferItem selected_item = this.ListViewTransfer.SelectedItem as ListViewTransferItem;
            if (selected_item == null) { return; }
            if (selected_item.IsDirectory)
            {
                /// 双击文件夹
                PageViewModel.ListViewOpenOrCloseDirectory(selected_item);
            }
            else
            {
                /// 双击文件, todo 显示文件细节信息
            }
        }
    }
}
