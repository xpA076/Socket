using FileManager.SocketLib;
using FileManager.Static;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FileManager.Models.TransferLib.Services
{
    public class TransferThreadPool
    {
        private readonly int[] RetryInterval = new int[] { 5, 5, 10, 10, 20, 60, 300 };

        private const int DefaultThreadLimit = 16;

        public int ThreadLimit { get; private set; }

        private readonly List<Thread> Threads = new List<Thread>();

        private readonly List<TransferThreadInfo> ThreadInfos = new List<TransferThreadInfo>();

        private readonly TransferDiskManager DiskManager = new TransferDiskManager();

        private readonly PacketIndexGenerator IndexGenerator = new PacketIndexGenerator();

        private ConnectionRoute Route = null;

        private TransferInfoFile CurrentFile = null;

        /// <summary>
        /// 设置此 Flag 为 true 可以终结下载子线程
        /// </summary>
        private bool IsStopTransfer = false;

        /// <summary>
        /// 确定是否处在确定连接状态, 即 TryUntilConnectionAvailable() 是否在运行
        /// </summary>
        private bool IsTryingToConnect = false;

        private readonly object TryingToConnectLock = new object();

        private ManualResetEvent ConnectionAvailableEvent = new ManualResetEvent(false);


        public TransferThreadPool() : this(DefaultThreadLimit)
        {

        }

        public TransferThreadPool(int thread_limt)
        {
            ThreadLimit = thread_limt;
            for (int i = 0; i < ThreadLimit; ++i)
            {
                Threads.Add(null);
                ThreadInfos.Add(null);
            }
        }


        /// <summary>
        /// 设置线程池中子线程参数, 并阻塞等待传输任务
        /// </summary>
        public void InitializeThreads()
        {
            TerminateAllThreads();
            for (int i = 0; i < ThreadLimit; ++i)
            {
                Thread thread = new Thread(new ParameterizedThreadStart(TransferThreadUnit)) { IsBackground = true };
                TransferThreadInfo info = new TransferThreadInfo();
                Threads[i] = thread;
                ThreadInfos[i] = info;
                thread.Start(info);
                //Thread.Sleep(10);
            }
        }


        /// <summary>
        /// 终止所有线程等待, 并结束线程任务
        /// </summary>
        public void TerminateAllThreads()
        {
            for (int i = 0; i < ThreadLimit; ++i)
            {
                if (ThreadInfos[i] != null)
                {
                    ThreadInfos[i].IsExit = true;
                    ThreadInfos[i].Signal.Set();
                    ThreadInfos[i].Signal.WaitOne();
                }
            }
        }


        /// <summary>
        /// 下载调度线程, 同步阻塞至当前任务完成
        /// </summary>
        /// <param name="file"></param>
        public void DownloadOne(TransferInfoFile file)
        {
            /// 初始化各辅助类
            CurrentFile = file;
            IndexGenerator.Reset();
            IndexGenerator.TotalIndex = (CurrentFile.Length - 1) / TransferDiskManager.BlockSize + 1;
            DiskManager.SetPath(CurrentFile.LocalPath, FileAccess.Write);
            /// 启动子线程
            int thread_count = GetThreadCount(CurrentFile.Length);
            for (int i = 0; i < thread_count; ++i)
            {
                ThreadInfos[i].Signal.Set();
            }
            for (int i = 0; i < thread_count; ++i)
            {
                ThreadInfos[i].Signal.WaitOne();
            }
            
            /// 向sever端发出请求, 释放文件

            /// 判断任务是否正确完成
        }





        private void TransferThreadUnit(object o)
        {
            TransferThreadInfo info = (TransferThreadInfo)o;

            SocketClient client = null;
            long packet;
            /// 主循环, 离开循环线程即终结
            while (true)
            {
                info.Signal.WaitOne();
                if (info.IsExit)
                {
                    info.Signal.Set();
                    break;
                }
                /// 在 TransferThreadInfo 唤醒该线程后的单个文件任务传输循环
                while (true)
                {
                    /// todo 在这里响应其它线程终止下载任务行为
                    /// 类比 TileTasksManager.DownloadThreadUnit()
                    
                    /// 获取 packet index, 已完成则退出单文件传输循环
                    packet = IndexGenerator.GenerateIndex();
                    if (packet < 0)
                    {
                        break;
                    }
                    /// 确定 / 获取可用的 SocketClient
                    if (client == null)
                    {
                        while (!IsStopTransfer)
                        {
                            Task.Run(() => { TryUntilConnectionAvailable(); });
                            ConnectionAvailableEvent.WaitOne();
                            try
                            {
                                client = SocketFactory.Instance.GenerateConnectedSocketClient(Route);
                                break;
                            }
                            catch (Exception)
                            {
                                ConnectionAvailableEvent.Reset();
                                continue;
                            }
                        }
                    }

                }

            }
        }


        private void TryUntilConnectionAvailable()
        {
            lock (TryingToConnectLock)
            {
                if (IsTryingToConnect)
                {
                    return;
                }
                else
                {
                    IsTryingToConnect = true;
                }
            }
            int i = 0;
            while (!IsStopTransfer)
            {
                int seconds;
                if (i < RetryInterval.Length)
                {
                    seconds = RetryInterval[i];
                }
                else
                {
                    seconds = RetryInterval.Last();
                }
                try
                {
                    SocketFactory.Instance.Request(SocketLib.Enums.HB32Packet.None, new byte[1]);
                    ConnectionAvailableEvent.Set();
                    break;
                }
                catch (Exception)
                {
                    Thread.Sleep(seconds * 1000);
                    ++i;
                }
            }
            IsTryingToConnect = false;
        }


        private int GetThreadCount(long length)
        {
            if (length <= (4 << 10))
            {
                return 1;
            }
            else if (length <= (16 << 10))
            {
                return 2;
            }
            else if (length <= (128 << 20))
            {
                return 4;
            }
            else
            {
                return 16;
            }
        }

    }
}
