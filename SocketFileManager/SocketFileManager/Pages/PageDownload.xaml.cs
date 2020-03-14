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
            this.ListBoxTask.ItemsSource = fileTasks;
        }

        private List<FileTask> fileTasks = new List<FileTask>();
        private int currentTaskIndex;
        private FileStream localFileStream = null;

        public int SmallFileLimit = 4 * 1024 * 1024;
        public int ThreadLimit = 10;

        private bool isDownloading = false;
        
        private FileTask CurrentTask
        {
            get
            {
                return fileTasks[currentTaskIndex];
            }
        }


        public void AddDownloadTask(FileTask downloadTask)
        {
            lock (fileTasks)
            {
                fileTasks.Add(downloadTask);
                if (!isDownloading)
                {
                    // 启动下载
                    Thread th = new Thread(Download);
                    th.IsBackground = true;
                    th.Start();
                }
            }
        }

        /// <summary>
        /// 下载的主线程，循环下载列表中每一个任务直至所有任务完成
        /// </summary>
        private void Download()
        {
            isDownloading = true;
            // 界面更新 0%
            // do sth
            // 直到 currentTaskIndex 指向最后，代表所有任务完成
            while (currentTaskIndex < fileTasks.Count)
            {
                // 界面更新当前任务
                // do sth
                DownloadSingleTask(fileTasks[currentTaskIndex]);
            }
            // 界面更新 100%
            // do sth
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
                SokcetFileClass[] files = this.parent.RequestDirectory(task.RemotePath);
                lock (this.fileTasks)
                {
                    this.fileTasks.RemoveAt(currentTaskIndex);
                    List<FileTask> tasks = new List<FileTask>();
                    foreach (SokcetFileClass f in files)
                    {
                        tasks.Add(new FileTask
                        {
                            IsDirectory = f.IsDirectory,
                            Type = "download",
                            RemotePath = task.RemotePath + "\\" + f.Name,
                            LocalPath = task.LocalPath + "\\" + f.Name,
                            Length = f.Length,
                        });
                    }
                    this.fileTasks.InsertRange(currentTaskIndex, tasks);
                }
                return;
            }
            #endregion

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
                        fileTasks[0].RemotePath);
                    client.ReceiveBytes(client.client, out HB32Header header, out byte[] bytes);
                    client.Close();
                    if (header.Flag != SocketDataFlag.DownloadAllowed)
                    {
                        throw new Exception(Encoding.UTF8.GetString(bytes));
                    }
                    File.WriteAllBytes(task.LocalPath, bytes);
                    currentTaskIndex++;
                    return;
                }
                catch (Exception ex)
                {
                    // todo: task status 改为 fail 再返回
                    System.Windows.Forms.MessageBox.Show(ex.Message);
                    currentTaskIndex++;
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
                    fileTasks[0].RemotePath);
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
                // todo: task status 改为 fail 再返回
                System.Windows.Forms.MessageBox.Show(ex.Message);
                return;
            }

            // 启动下载子线程
            task.ServerId = int.Parse(response);
            localFileStream = new FileStream(task.LocalPath, FileMode.OpenOrCreate, FileAccess.Write);
            lock (this.packageRecord)
            {
                packageRecord[0] = -1;
                packageRecord[1] = (int)(task.Length / HB32Encoding.DataSize) +
                    (task.Length % HB32Encoding.DataSize > 0 ? 1 : 0);
            }
            Thread[] threads = new Thread[ThreadLimit];
            for (int i = 0; i < ThreadLimit; ++i)
            {
                threads[i] = new Thread(DownloadThreadUnit);
                threads[i].IsBackground = true;
                threads[i].Start();
            }
            for (int i = 0; i < ThreadLimit; ++i)
            {
                threads[i].Join();
            }
            localFileStream.Close();
            localFileStream = null;
            currentTaskIndex++;
            #endregion
        }

        /// <summary>
        /// 多线程下载的单元线程
        /// 循环获取当前线程工作包序列，从远程 server 获取字节信息并写入
        /// </summary>
        private void DownloadThreadUnit()
        {
            int id = fileTasks[currentTaskIndex].ServerId;
            SocketClient client = GetConnectedSocketClient();
            for (int package = GetPackageIndex(); package != -1; package = GetPackageIndex())
            {
                while (true)
                {
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
                        break;
                    }
                    catch (Exception)
                    {
                        client = GetConnectedSocketClient();
                    }
                }
            }
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
        /// packageRecord[0] : 当前最后一个 package index
        /// packageRecord[1] : 当前 task 需要的总 package
        /// </summary>
        private int[] packageRecord = new int[2];
        /// <summary>
        /// 申请获取任务package index, 任务完成则返回 -1
        /// </summary>
        /// <returns> package index </returns>
        private int GetPackageIndex()
        {
            lock (this.packageRecord)
            {
                if (packageRecord[0] == packageRecord[1])
                {
                    return -1;
                }
                else
                {
                    packageRecord[0]++;
                    return packageRecord[0];
                }
            }
        }
    }


}
