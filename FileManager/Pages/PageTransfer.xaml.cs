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
using FileManager.Models.TransferLib.Info;
using FileManager.Models.TransferLib.Services;

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
            PageViewModel.InfoRoots = this.InfoRoots;
            this.ListViewTransfer.ItemsSource = PageViewModel.ListViewItems;
            this.DataContext = PageViewModel;
        }
        
               
        private PageTransferViewModel PageViewModel { get; set; } = new PageTransferViewModel();

        private TransferThreadPool TransferThreadPool;

        private readonly List<TransferInfoRoot> InfoRoots = new List<TransferInfoRoot>();

        private readonly List<TransferManager> TransferManagers = new List<TransferManager>();

        public bool IsTransfering
        {
            get
            {
                foreach (TransferManager m in TransferManagers)
                {
                    if (m.IsTransfering)
                    {
                        return true;
                    }
                }
                return false;
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
            if (this.IsTransfering)
            {
                InfoRoots.Add(rootInfo);
            }
            else
            {
                InfoRoots.Clear();
                TransferManagers.Clear();
                InfoRoots.Add(rootInfo);
                OnTransferManagerFinished(null, null);
            }
        }


        /// <summary>
        /// 若调用方为 null, 则为从 AddTransferTask() 调用, 此时从首个 InfoRoot 开始任务
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnTransferManagerFinished(object sender, EventArgs e)
        {
            int idx;
            if (sender == null)
            {
                idx = 0;
            }
            else
            {
                TransferManager m = sender as TransferManager;
                for (idx = 0; idx < InfoRoots.Count; ++idx)
                {
                    if (m == TransferManagers[idx])
                    {
                        idx++;
                        break;
                    }
                }
            }
            /// 此时 idx 指向首个未完成 InfoRoot 节点索引
            if (idx < InfoRoots.Count)
            {
                TransferManager tm = new TransferManager(InfoRoots[idx]);
                TransferThreadPool.Route = InfoRoots[idx].Route;
                tm.TransferThreadPool = TransferThreadPool;
                tm.PageViewModel = PageViewModel;
                tm.TransferFinishedCallback += OnTransferManagerFinished;
                PageViewModel.SetRoot(InfoRoots[idx]);
                TransferManagers.Add(tm);
                tm.InitTransfer();
            }
            else
            {
                /// 全部传输完成, 可在此做后续处理
            }
        }

    }
}
