using FileManager.Models;
using FileManager.Models.TransferLib;
using FileManager.ViewModels;
using FileManager.SocketLib;
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
            this.ListViewTransfer.ItemsSource = PageViewModel.ListViewItems;
            this.DataContext = PageViewModel;
        }
        
               
        private TransferManager TransferManager { get; set; }

        private PageTransferViewModel PageViewModel { get; set; } = new PageTransferViewModel();



        public bool IsTransfering { get { return TransferManager.IsTransfering; } }


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
            throw new NotImplementedException();
        }

        private void ButtonResume_Click(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        public void AddTransferTask(TransferInfoRoot rootInfo)
        {
            TransferManager = new TransferManager(rootInfo);
            PageViewModel.SetRoot(rootInfo);
            TransferManager.PageViewModel = PageViewModel;
            /// UI 更新事件调用
            TransferManager.InitTransfer();
        }



    }
}
