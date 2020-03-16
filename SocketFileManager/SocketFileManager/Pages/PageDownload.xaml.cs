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

using SocketFileManager.Models;
using SocketFileManager.SocketLib;

namespace SocketFileManager.Pages
{
    /// <summary>
    /// PageDownload.xaml 的交互逻辑
    /// </summary>
    public partial class PageDownload : Page
    {
        private MainWindow parent;

        public PageDownload(MainWindow parent)
        {
            this.parent = parent;
            InitializeComponent();
            // buttons
            this.ButtonPause.PreviewMouseLeftButtonDown += new MouseButtonEventHandler(ButtonPause_Click);
            this.ButtonStart.PreviewMouseLeftButtonDown += new MouseButtonEventHandler(ButtonStart_Click);
            this.ListBoxTask.ItemsSource = transfer.FileTasks;
            this.GridProgress.DataContext = progressView;
        }

        #region 下载和 UI 相关 private 变量
        private bool showCurrentPercent = true;
        private bool showTotalPercent = true;

        private ProgressViewModel progressView = new ProgressViewModel();

        private TransferRecord transfer = new TransferRecord();

        //private List<FileTask> fileTasks = new List<FileTask>();
        //private int currentTaskIndex;
        /*
        private Dictionary<string, long> byteCount = new Dictionary<string, long>()
        {
            { "total_length", 0 },

            { "current_length", 0 },
            { "current_finished", 0 },

            { "task_addup", 0}, 
            { "last_time", 0 }, // 上次更新时间
            { "new_bytes", 0 }, // 上次更新以后传输字节数
        };
        
        /// <summary>
        /// packageRecord[0] : 当前最后一个 package index
        /// packageRecord[1] : 当前 task 需要的总 package
        /// </summary>
        private Dictionary<string, int> packageRecord = new Dictionary<string, int>()
        {
            { "last", -1 },
            { "total", 0 },
        };
        */
        private FileStream localFileStream = null;
        #endregion

        public int SmallFileLimit = 4 * 1024 * 1024;
        public int ThreadLimit = 10;

        private bool isDownloading = false;
        private bool stopDownloading = false;

        private void ButtonPause_Click(object sender, MouseButtonEventArgs e)
        {
            lock (this.transfer)
            {
                transfer.Save();
            }
            stopDownloading = true;
        }

        private void ButtonStart_Click(object sender, MouseButtonEventArgs e)
        {
            lock (this.transfer)
            {
                transfer = new TransferRecord();
                transfer.Load(); 
            }
            if (this.parent.ServerIP == null)
            {
                this.parent.ServerIP = System.Net.IPAddress.Parse(Config.LastConnect);
            }
            if (!isDownloading)
            {
                // 启动下载
                Thread th = new Thread(Download);
                th.IsBackground = true;
                th.Start();
            }
        }



        /// <summary>
        /// browser 页面添加任务响应事件
        /// </summary>
        /// <param name="downloadTask">文件/文件夹任务</param>
        public void AddDownloadTask(FileTask downloadTask)
        {
            lock (this.transfer)
            {
                transfer.AddTask(downloadTask);
                transfer.TotalLength += downloadTask.Length;
            }
            if (!isDownloading)
            {
                // 启动下载
                Thread th = new Thread(Download);
                th.IsBackground = true;
                th.Start();
            }
        }

        /// <summary>
        /// 下载的主线程，循环下载列表中每一个任务直至所有任务完成
        /// </summary>
        private void Download()
        {
            isDownloading = true;
            // 界面更新 0%
            //UpdateProgerss(0);
            // 直到 currentTaskIndex 指向最后，代表所有任务完成
            while (!transfer.IsFinished())
            {
                // 界面更新当前任务
                if (!transfer.CurrentTask.IsDirectory)
                {
                    // Directory 有大小, 若对 directory 任务更新UI会导致任务大小重复计算
                    transfer.RecordNewTask();
                    UpdateTaskProgress();
                }
                // 启动下载
                DownloadSingleTask(transfer.CurrentTask);
                if (stopDownloading)
                {
                    stopDownloading = false;
                    isDownloading = false;
                    return;
                }
                // 完成下载
                transfer.FinishCurrentTask();
            }
            // 界面更新 100%
            allFin = allLen;
            curFin = curLen;
            UpdateUI(false);
            transfer.Clear();
            isDownloading = false;
        }

        /// <summary>
        /// 启动下载任务，根据当前 FileTask 任务区分下载模式，并阻塞直到所有子线程完成后
        ///  将 currentTaskIndex 指向下个 task，返回
        /// 当前任务为 directory 则展开 directory ，更新 fileTasks 和 currentTaskIndex 后返回
        /// 当前任务为 小文件 则启用单线程下载，currentTaskIndex++ 后返回
        /// 当前任务为 大文件 则启用多线程下载，currentTaskIndex++ 后返回
        /// </summary>
        private void DownloadSingleTask(FileTask task)
        {
            #region 当前任务为 directory
            if (task.IsDirectory)
            {
                if (!Directory.Exists(task.LocalPath))
                {
                    DirectoryInfo dirInfo = new DirectoryInfo(task.LocalPath);
                    dirInfo.Create();
                }
                SokcetFileClass[] files;
                try { files = this.parent.RequestDirectory(task.RemotePath); }
                catch (Exception)
                {
                    task.Status = "denied";
                    return;
                }
                this.Dispatcher.Invoke(new Action(() => {
                    lock (this.transfer)
                    {
                        this.transfer.FileTasks.RemoveAt(transfer.CurrentTaskIndex);
                        List<FileTask> tasks = new List<FileTask>();
                        int bias = transfer.CurrentTaskIndex;
                        for (int i = 0; i < files.Length; ++i)
                        {
                            SokcetFileClass f = files[i];
                            this.transfer.FileTasks.Insert(bias + i, new FileTask
                            {
                                IsDirectory = f.IsDirectory,
                                Type = "download",
                                RemotePath = task.RemotePath + "\\" + f.Name,
                                LocalPath = task.LocalPath + "\\" + f.Name,
                                Length = f.Length,
                            });
                        }
                    }
                }));
                return;
            }
            #endregion

            task.Status = "download";

            #region 单线程下载
            if (task.Length <= SmallFileLimit)
            {
                try
                {
                    SocketClient client = new SocketClient(this.parent.ServerIP, this.parent.ServerPort);
                    client.Connect();
                    client.SendBytes(client.client,
                        new HB32Header
                        {
                            Flag = SocketDataFlag.DownloadRequest,
                            I3 = 1, // I3 = 1 代表小文件下载
                        },
                        task.RemotePath);
                    client.ReceiveBytes(client.client, out HB32Header header, out byte[] bytes);
                    client.Close();
                    if (header.Flag != SocketDataFlag.DownloadAllowed)
                    {
                        throw new Exception(Encoding.UTF8.GetString(bytes));
                    }
                    File.WriteAllBytes(task.LocalPath, bytes);
                    this.transfer.AddFinishedBytes(bytes.Length);
                    task.Status = "success";
                    transfer.CurrentTaskIndex++;
                    return;
                }
                catch (Exception ex)
                {
                    task.Status = "failed";
                    System.Windows.Forms.MessageBox.Show(ex.Message);
                    transfer.CurrentTaskIndex++;
                    return;
                }
            }
            #endregion

            #region 多线程下载
            string response;
            try
            {
                SocketClient client = new SocketClient(this.parent.ServerIP, this.parent.ServerPort);
                client.Connect();
                client.SendBytes(client.client,
                    new HB32Header { Flag = SocketDataFlag.DownloadRequest },
                    task.RemotePath);
                client.ReceiveBytes(client.client, out HB32Header header, out byte[] bytes);
                client.Close();
                if (header.Flag != SocketDataFlag.DownloadAllowed)
                {
                    throw new Exception(Encoding.UTF8.GetString(bytes));
                }
                response = Encoding.UTF8.GetString(bytes);
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.Message);
                task.Status = "failed";
                transfer.CurrentTaskIndex++;
                return;
            }

            // 启动下载子线程
            task.ServerId = int.Parse(response);
            localFileStream = new FileStream(task.LocalPath, FileMode.OpenOrCreate, FileAccess.Write);
            /*
            lock (this.transfer)
            {
                transfer.LastPackage = -1;
                transfer.TotalPackage = (int)(task.Length / HB32Encoding.DataSize) +
                    (task.Length % HB32Encoding.DataSize > 0 ? 1 : 0);
            }
            */
            Thread[] threads = new Thread[ThreadLimit];
            for (int i = 0; i < ThreadLimit; ++i)
            {
                threads[i] = new Thread(DownloadThreadUnit);
                threads[i].IsBackground = true;
                threads[i].Start();
                Thread.Sleep(50);
            }
            for (int i = 0; i < ThreadLimit; ++i)
            {
                // 阻塞至子线程工作完毕
                threads[i].Join();
            }
            localFileStream.Close();
            localFileStream = null;
            // 请求server端关闭并释放文件
            SocketClient sc = GetConnectedSocketClient();
            sc.SendHeader(sc.client,
                new HB32Header
                {
                    Flag = SocketDataFlag.DownloadPackageRequest,
                    I1 = transfer.CurrentTask.ServerId,
                    I2 = -1,
                });
            sc.Close();

            if (stopDownloading)
            {
                task.Status = "pause";
                return;
            }

            task.Status = "success";
            transfer.CurrentTaskIndex++;
            #endregion
        }

        /// <summary>
        /// 多线程下载的单元线程
        /// 循环获取当前线程工作包序列，从远程 server 获取字节信息并写入
        /// </summary>
        private void DownloadThreadUnit()
        {
            int id = transfer.CurrentTask.ServerId;
            SocketClient client = GetConnectedSocketClient();
            for (int package = GetPackageIndex(); package != -1; package = GetPackageIndex())
            {
                while (true)
                {
                    if (stopDownloading)
                    {
                        client.Close();
                        return;
                    }
                    try
                    {
                        client.SendHeader(client.client,
                            new HB32Header
                            {
                                Flag = SocketDataFlag.DownloadPackageRequest,
                                I1 = id,
                                I2 = package,
                            });
                        client.ReceivePackage(client.client, out HB32Header header, out byte[] bytes);
                        lock (this.localFileStream)
                        {
                            localFileStream.Seek((long)package * HB32Encoding.DataSize, SeekOrigin.Begin);
                            localFileStream.Write(bytes, 0, header.ValidByteLength);
                        }
                        lock (this.transfer)
                        {
                            transfer.AddFinishedBytes(header.ValidByteLength);
                        }
                        break;
                    }
                    catch (Exception)
                    {
                        client = GetConnectedSocketClient();
                    }
                }
            }
            client.Close();
        }

        /// <summary>
        /// 获取已连接成功的 SocketClient 对象
        /// 若连接失败则在 3s后 重启新连接直至连接成功
        /// </summary>
        /// <returns></returns>
        private SocketClient GetConnectedSocketClient()
        {
            while (true)
            {
                try
                {
                    SocketClient client = new SocketClient(this.parent.ServerIP, this.parent.ServerPort);
                    client.Connect();
                    return client;
                }
                catch (Exception)
                {
                    Thread.Sleep(3000);
                }
            }
        }

        /// <summary>
        /// 申请获取任务package index, 任务完成则返回 -1
        /// 根据 package 数目更新 UI
        /// </summary>
        /// <returns> package index </returns>
        private int GetPackageIndex()
        {
            lock (this.transfer)
            {
                if (transfer.CurrentTask.LastPackage + 1 == transfer.CurrentTask.TotalPackage)
                {
                    return -1;
                }
                else
                {
                    transfer.CurrentTask.LastPackage++;
                    if (transfer.AllowUpdate()) { UpdateUI(); }
                    return transfer.CurrentTask.LastPackage;
                }
            }
        }

        /// <summary>
        /// 在启动每个新任务前调用
        /// </summary>
        /// <param name="currentTask"></param>
        private void UpdateTaskProgress()
        {
            lock (this.transfer)
            {
                transfer.RecordNewTask();
            }
            UpdateUI();
        }


        private long curFin;
        private long curLen;
        private long allFin;
        private long allLen;
        /// <summary>
        /// 更新 UI
        /// </summary>
        /// <param name="getData"> 是否从 TransferRecord 中获取数据更新 speed 等时间信息 </param>
        private void UpdateUI(bool getData = true)
        {
            if (getData)
            {
                double speed;
                lock (transfer)
                {
                    curFin = transfer.CurrentFinished;
                    curLen = transfer.CurrentLength;
                    allFin = transfer.TotalFinished;
                    allLen = transfer.TotalLength;
                    speed = transfer.GetSpeed();
                }
                int seconds = (int)((allLen - allFin) / speed);
                progressView.Speed = num2text(speed).PadLeft(18, ' ') + "/s";
                progressView.TimeRemaining = (seconds / 3600).ToString().PadLeft(10, ' ') +
                    ": " + (seconds % 3600 / 60).ToString().PadLeft(2, '0') +
                    ": " + (seconds % 60).ToString().PadLeft(2, '0');
            }
            if (showCurrentPercent)
            {
                progressView.CurrentProgress =
                    ((double)curFin * 100 / curLen).ToString("0.00").PadLeft(16, ' ') + " %";
            }
            else
            {
                progressView.CurrentProgress =
                    num2text(curFin).PadLeft(12, ' ') + "/" + num2text(curLen);
            }
            if (showTotalPercent)
            {
                progressView.TotalProgress =
                    ((double)allFin * 100 / allLen).ToString("0.00").PadLeft(16, ' ') + " %";
            }
            else
            {
                progressView.TotalProgress =
                    num2text(allFin).PadLeft(12, ' ') + "/" + num2text(allLen);
            }
        }

        private string num2text(double num)
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
    }


}
