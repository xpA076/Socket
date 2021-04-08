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

using FileManager.Models;
using FileManager.ViewModels;
using SocketLib;

namespace FileManager.Pages
{
    /// <summary>
    /// PageDownload.xaml 的交互逻辑
    /// </summary>
    public partial class PageTransfer : Page
    {
        private MainWindow parent;

        public PageTransfer(MainWindow parent)
        {
            this.parent = parent;
            InitializeComponent();
            // buttons
            //this.ButtonPause.PreviewMouseLeftButtonDown += new MouseButtonEventHandler(ButtonPause_Click);
            //this.ButtonStart.PreviewMouseLeftButtonDown += new MouseButtonEventHandler(ButtonStart_Click);
            this.ListViewTask.ItemsSource = FTManager.FileTasks;
            this.GridProgress.DataContext = ProgressView;
            FTManager.UpdateUICallback = new PageUICallback(this.UpdateUI);
            FTManager.UpdateProgressCallback = new PageUICallback(this.UpdateProgress);
            FTManager.UpdateTasklistCallback = new PageUIInvokeCallback((Action a) => { this.Dispatcher.Invoke(a); });
        }

        #region 下载和 UI 相关 private 变量
        private bool showCurrentPercent = true;
        private bool showTotalPercent = true;


        private readonly ProgressViewModel ProgressView = new ProgressViewModel();
            

        private FileTaskManager FTManager { get; set; } = new FileTaskManager();

        #endregion

        public bool IsTransfering { get { return FTManager.IsTransfering; } }

        private void GridCurrentProgress_Click(object sender, MouseButtonEventArgs e)
        {
            showCurrentPercent = !showCurrentPercent;
            UpdateProgress();
        }
        private void GridTotalProgress_Click(object sender, MouseButtonEventArgs e)
        {
            showTotalPercent = !showTotalPercent;
            UpdateProgress();
        }

        public void ButtonPause_Click(object sender, RoutedEventArgs e)
        {
            FTManager.Pause();
        }

        private void ButtonResume_Click(object sender, RoutedEventArgs e)
        {
            FTManager.Load();
            this.ListViewTask.ItemsSource = FTManager.FileTasks;
            // ************* todo *******************
            // 确定目前是否有 severIP 2020.02.12 ***********
            if (!IsTransfering) { FTManager.InitDownload(); }
        }


        /// <summary>
        /// browser 页面添加下载任务的响应事件
        /// </summary>
        /// <param name="downloadTask">文件/文件夹任务</param>
        public void AddTask(FileTask task)
        {
            FTManager.AddTask(task);
            if (!IsTransfering) { FTManager.InitDownload(); }
        }


        public void AddTasks(List<FileTask> fileTasks)
        {
            foreach (FileTask task in fileTasks)
            {
                FTManager.AddTask(task);
            }
            if (!IsTransfering) { FTManager.InitDownload(); }
        }




        #region UI更新 
        /// 作为 Callback 在 FileTaskManager 中调用
        /// <summary>
        /// 更新 UI 
        /// </summary>
        private void UpdateUI()
        {
            UpdateSpeed();
            UpdateProgress();
        }

        private void UpdateSpeed()
        {
            double speed = FTManager.GetSpeed();
            int seconds = (int)((this.FTManager.TotalLength - this.FTManager.TotalFinished) / speed);
            ProgressView.Speed = Size2String(speed).PadLeft(18, ' ') + "/s";
            ProgressView.TimeRemaining = (seconds / 3600).ToString().PadLeft(10, ' ') +
                ": " + (seconds % 3600 / 60).ToString().PadLeft(2, '0') +
                ": " + (seconds % 60).ToString().PadLeft(2, '0');
        }

        private void UpdateProgress()
        {
            long cf = this.FTManager.CurrentFinished;
            long cl = this.FTManager.CurrentLength;
            long tf = this.FTManager.TotalFinished;
            long tl = this.FTManager.TotalLength;

            if (showCurrentPercent)
            {
                if (cl == 0)
                {
                    ProgressView.CurrentProgress = "--";
                }
                else
                {
                    ProgressView.CurrentProgress = ((double)cf * 100 / cl).ToString("0.00").PadLeft(16, ' ') + " %";
                }
            }
            else
            {
                ProgressView.CurrentProgress = Size2String(cf).PadLeft(12, ' ') + "/" + Size2String(cl);
            }
            if (showTotalPercent)
            {
                if (tl == 0)
                {
                    ProgressView.TotalProgress = "--";
                }
                else
                {
                    ProgressView.TotalProgress = ((double)tf * 100 / tl).ToString("0.00").PadLeft(16, ' ') + " %";
                }
            }
            else
            {
                ProgressView.TotalProgress = Size2String(tf).PadLeft(12, ' ') + "/" + Size2String(tl);
            }
            if (tf == tl)
            {
                ProgressView.TimeRemaining = "        00: 00: 00";
            }
        }

        private string Size2String(double num)
        {
            if (num > 1 << 30)
            {
                double d = num / (1 << 30);
                return d.ToString("0.00") + " G";
            }
            else if (num > 1 << 20)
            {
                double d = num / (1 << 20);
                return d.ToString("0.00") + " M";
            }
            else if (num > 1 << 10)
            {
                double d = num / (1 << 10);
                return d.ToString("0.00") + " K";
            }
            else
            {
                return num.ToString("0.00") + " B";
            }
        }

        #endregion
    }
}
