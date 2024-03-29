﻿using FileManager.Events.UI;
using FileManager.Exceptions;
using FileManager.Models.Serializable;
using FileManager.Models.Serializable.HeartBeat;
using FileManager.Models.SocketLib.Models;
using FileManager.Models.SocketLib.SocketIO;
using FileManager.Models.TransferLib.Enums;
using FileManager.Models.TransferLib.Info;
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
    /// <summary>
    /// 由 PageTransfer 管理, 负责一个 Route 下的传输子线程调度
    /// </summary>
    public class TransferThreadPool
    {
        public ConnectionRoute Route = null;

        private readonly int[] RetryInterval = new int[] { 5, 5, 10, 10, 20, 60, 300 };

        private const int DefaultThreadLimit = 1;

        public int ThreadLimit { get; private set; }

        private readonly Thread[] Threads;

        private readonly AutoResetEvent[] SubThreadSignals;

        private readonly AutoResetEvent[] MainThreadSignals;

        private readonly TransferDiskManager DiskManager = new TransferDiskManager();

        private readonly PacketIndexGenerator IndexGenerator = new PacketIndexGenerator();

        private TransferInfoFile CurrentFile = null;

        private TransferType CurrentTransferType;


        /// <summary>
        /// 设置此 Flag 为 true 可以终结下载子线程
        /// </summary>
        private bool IsStopTransfer = false;

        /// <summary>
        /// 在 TerminateAllThreads() 中调用, 通过此 Flag 向子线程传递退出主循环信号
        /// </summary>
        private bool IsTerminateThreads = false;

        /// <summary>
        /// 确定是否处在确定连接状态, 即 TryUntilConnectionAvailable() 是否在运行
        /// </summary>
        private bool IsTryingToConnect = false;

        private readonly object TryingToConnectLock = new object();

        private ManualResetEvent ConnectionAvailableSignal = new ManualResetEvent(false);

        /// <summary>
        /// 子线程单元完成每个 packet 后调用, 用于计数器更新UI
        /// </summary>
        public event FinishBytesEventHandler UIFinishBytes;


        public TransferThreadPool() : this(DefaultThreadLimit)
        {

        }

        public TransferThreadPool(int thread_limit)
        {
            ThreadLimit = thread_limit;
            Threads = new Thread[thread_limit];
            SubThreadSignals = new AutoResetEvent[thread_limit];
            MainThreadSignals = new AutoResetEvent[thread_limit];
            for (int i = 0; i < ThreadLimit; ++i)
            {
                Threads[i] = null;
                SubThreadSignals[i] = new AutoResetEvent(false);
                MainThreadSignals[i] = new AutoResetEvent(false);
            }
        }


        /// <summary>
        /// 设置线程池中子线程参数, 启动子线程并阻塞等待传输任务调用
        /// </summary>
        public void InitializeThreads()
        {
            TerminateAllThreads();
            IsStopTransfer = false;
            for (int i = 0; i < ThreadLimit; ++i)
            {
                Thread thread = new Thread(new ParameterizedThreadStart(TransferThreadUnit)) { IsBackground = true };
                Threads[i] = thread;
                SubThreadSignals[i].Reset();
                MainThreadSignals[i].Reset();
                thread.Start(i);
            }
        }


        /// <summary>
        /// 响应外部 UI 调用的传输暂停事件
        /// 所有子线程任务退出 while 循环并终止, 将子线程指向 null
        /// DownloadOne() / UploadOne() 执行结束并返回至调用方
        /// </summary>
        public void Pause()
        {
            IsStopTransfer = true;
        }


        /// <summary>
        /// 正确结束所有任务后, 释放所有子线程并将 ThreadPool 重置为未初始化状态
        /// </summary>
        public void Finish()
        {
            TerminateAllThreads();
            IsStopTransfer = false;
        }


        /// <summary>
        /// 依次终止所有线程等待, 并结束线程任务, 将线程引用指向 null
        /// </summary>
        private void TerminateAllThreads()
        {
            IsTerminateThreads = true;
            for (int i = 0; i < ThreadLimit; ++i)
            {
                if (Threads[i] != null)
                {
                    SubThreadSignals[i].Set();
                    MainThreadSignals[i].WaitOne();
                    Threads[i] = null;
                }
            }
            IsTerminateThreads = false;
        }


        /// <summary>
        /// 下载调度线程, 同步阻塞至当前任务完成
        /// </summary>
        /// <param name="file"></param>
        public void TransferOne(TransferInfoFile file, TransferType type)
        {
            /// 重置 Flag
            IsStopTransfer = false;

            /// 初始化各辅助类
            CurrentFile = file;
            CurrentTransferType = type;
            IndexGenerator.Clear();
            IndexGenerator.TotalIndex = (CurrentFile.Length - 1) / TransferDiskManager.BlockSize + 1;
            IndexGenerator.LastFinishedIndex = CurrentFile.FinishedPacket;
            if (type == TransferType.Download)
            {
                DiskManager.SetPath(CurrentFile.LocalPath, FileAccess.Write);
            }
            else
            {
                DiskManager.SetPath(CurrentFile.LocalPath, FileAccess.Read);
            }

            /// 启动子线程, 并阻塞直至所有子线程均通过信号释放控制权
            int thread_count = GetThreadCount(CurrentFile.Length);
            AutoResetEvent[] signals = new AutoResetEvent[thread_count];
            for (int i = 0; i < thread_count; ++i)
            {
                signals[i] = MainThreadSignals[i];
                SubThreadSignals[i].Set();
            }
            AutoResetEvent.WaitAll(signals);

            /// 结束本地文件写入
            DiskManager.Finish();

            /// 向sever端发出请求, 释放文件
            ReleaseFile(type, file.RemotePath);

            /// 判断任务是否正确完成
            /// 若因调用 Pause() 结束当前 File 传输任务, 则终止所有子线程, 返回调用方
            /// 否则保留子线程生存状态, 等待下一个任务信号
            if (IsStopTransfer)
            {
                TerminateAllThreads();
                CurrentFile.Status = TransferStatus.Pause;
            }
            else
            {
                CurrentFile.Status = TransferStatus.Finished;
            }
        }




        /// <summary>
        /// 子线程执行内容, while 循环等待外部线程调用 DownloadOne() 或 UploadOne()
        /// 阻塞等待信号执行对应任务
        /// </summary>
        /// <param name="o"></param>
        private void TransferThreadUnit(object o)
        {
            int idx = (int)o;
            AutoResetEvent sub_signal = SubThreadSignals[idx];
            AutoResetEvent main_signal = MainThreadSignals[idx];
            SocketClient client = null;
            long packet;
            /// 主循环, 离开循环线程即终结
            /// 在任务间歇, 线程被信号量阻塞, 不应退出主 while 循环
            while (true)
            {
                sub_signal.WaitOne();
                if (IsTerminateThreads)
                {
                    client?.Close();
                    main_signal.Set();
                    break;
                }
                /// 唤醒该线程后的单个文件任务传输循环
                /// 在 while 循环内调用 Signal.Set(), 会通过信号将控制权切换至管理线程
                while (true)
                {
                    /// 响应其它线程终止下载任务行为, 或任务失败应退出
                    if (IsStopTransfer || CurrentFile.Status == TransferStatus.Failed)
                    {
                        main_signal.Set();
                        break;
                    }
                    /// 获取 packet index, 已完成则退出单文件传输循环
                    packet = IndexGenerator.GenerateIndex();
                    if (packet < 0)
                    {
                        main_signal.Set();
                        break;
                    }
                    try
                    {
                        long count;
                        if (CurrentTransferType == TransferType.Download)
                        {
                            count = DownloadSinglePacket(packet, ref client);
                        }
                        else
                        {
                            count = UploadSinglePacket(packet, ref client);
                        }
                        CurrentFile.FinishedPacket = IndexGenerator.LastFinishedIndex;
                        UIFinishBytes(this, new FinishBytesEventArgs(count));
                    }
                    catch (SocketConnectionException)
                    {
                        /// 若因网络异常导致任务失败, 则释放对应 packet, 并正常结束函数
                        /// 下个 while 循环会尝试重启 SocketClient
                        IndexGenerator.ReleaseIndex(packet);
                    }
                    catch (Exception)
                    {
                        /// 其它原因导致的任务失败 (server 端拒绝 / 本地写入异常 / 等) 会标记任务失败
                        /// 所有子线程的下个 while 循环会直接退出并结束当前任务, 返回管理线程
                        CurrentFile.Status = TransferStatus.Failed;
                    }
                }
            }
        }


        /// <summary>
        /// 进行单个下载 packet 的请求和本地写入操作, 返回写入本地文件字节数
        /// 任务失败会抛出不同异常 (SocketConnectionException 或 Exception)
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="client"></param>
        /// <returns></returns>
        private long DownloadSinglePacket(long packet, ref SocketClient client)
        {
            BuildValidSocketClient(ref client);

            /// 构造 request
            DownloadRequest request = new DownloadRequest(); 
            request.Type = DownloadRequest.RequestType.QueryByPath;
            request.ViewPath = CurrentFile.RemotePath;
            request.Begin = packet * TransferDiskManager.BlockSize;
            request.Length = Math.Min(CurrentFile.Length - request.Begin, TransferDiskManager.BlockSize);
            DownloadResponse response;

            /// 向 server 端请求内容
            try
            {
                //HB32Response resp = SocketFactory.Request(client, SocketLib.Enums.PacketType.DownloadRequest, request.ToBytes());
                //response = DownloadResponse.FromBytes(resp.Bytes);
                byte[] bs = SocketFactory.Instance.Request(client, request);
                response = DownloadResponse.FromBytes(bs, 4);
            }
            catch
            {
                client = null;
                throw new SocketConnectionException();
            }

            /// 写入本地文件
            if (response.Type != DownloadResponse.ResponseType.BytesResponse)
            {
                throw new Exception("Download denied");
            }
            DiskManager.WriteBytes(request.Begin, response.Bytes);

            /// 任务完成, 返回写入字节数
            return response.Bytes.Length;
        }


        /// <summary>
        /// 进行单个上传 packet 本地读取和网络请求, 返回上传成功字节数
        /// 任务失败会抛出不同异常 (SocketConnectionException 或 Exception)
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="client"></param>
        /// <returns></returns>
        private long UploadSinglePacket(long packet, ref SocketClient client)
        {
             BuildValidSocketClient(ref client);

            /// 确定相关参数
            UploadRequest request = new UploadRequest();
            request.Type = UploadRequest.RequestType.ByPath;
            request.ViewPath = CurrentFile.RemotePath;
            request.Begin = packet * TransferDiskManager.BlockSize;
            request.Length = Math.Min(CurrentFile.Length - request.Begin, TransferDiskManager.BlockSize);

            /// 读取本地文件
            request.Bytes = DiskManager.ReadBytes(request.Begin, (int)request.Length);

            
            UploadResponse response;
            /// 网络请求
            try
            {
                //HB32Response resp = SocketFactory.Request(client, SocketLib.Enums.PacketType.UploadRequest, request.ToBytes());
                //response = UploadResponse.FromBytes(resp.Bytes);
                byte[] bs = SocketFactory.Instance.Request(client, request);
                response = UploadResponse.FromBytes(bs, 4);
            }
            catch
            {
                client = null;
                throw new SocketConnectionException();
            }

            /// 确认packet是否正确完成
            if (response.Type != UploadResponse.ResponseType.SuccessResponse)
            {
                throw new Exception("Upload denied");
            }

            return request.Bytes.Length;
        }


        /// <summary>
        /// 确定 / 获取可用的 SocketClient
        /// </summary>
        /// <param name="client"></param>
        private void BuildValidSocketClient(ref SocketClient client)
        {
            if (client == null)
            {
                while (!IsStopTransfer)
                {
                    Task.Run(() => { TryUntilConnectionAvailable(); });
                    ConnectionAvailableSignal.WaitOne();
                    try
                    {
                        client = SocketFactory.Instance.GenerateConnectedSocketClient(Route);
                        break;
                    }
                    catch (Exception)
                    {
                        ConnectionAvailableSignal.Reset();
                        continue;
                    }
                }
            }
        }


        private void ReleaseFile(TransferType type, string path)
        {
            try
            {
                ReleaseFileRequest request = new ReleaseFileRequest
                {
                    Type = ReleaseFileRequest.RequestType.Default,
                    From = type == TransferType.Download ? ReleaseFileRequest.ReleaseFrom.Download : ReleaseFileRequest.ReleaseFrom.Upload,
                    ViewPath = path
                };
                byte[] bs = SocketFactory.Instance.Request(request);
                ReleaseFileResponse response = ReleaseFileResponse.FromBytes(bs, 4);
                //HB32Response resp = SocketFactory.Instance.Request(SocketLib.Enums.PacketType.ReleaseFileRequest, request.ToBytes());
                //ReleaseFileResponse response = ReleaseFileResponse.FromBytes(resp.Bytes);
            }
            catch { }
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
                    HeartBeatRequest request = new HeartBeatRequest();
                    byte[] bs = SocketFactory.Instance.Request(request);
                    HeartBeatResponse response = HeartBeatResponse.FromBytes(bs, 4);
                    //SocketFactory.Instance.Request(SocketLib.Enums.PacketType.Null, new byte[1]);
                    ConnectionAvailableSignal.Set();
                    break;
                }
                catch (Exception)
                {
                    Thread.Sleep(seconds * 1000);
                    ++i;
                }
            }
            lock (TryingToConnectLock)
            {
                IsTryingToConnect = false;
            }
        }


        private int GetThreadCount(long length)
        {
            int count;
            if (length <= (64 << 10))
            {
                count = 1;
            }
            else if (length <= (4 << 20))
            {
                count = 4;
            }
            else
            {
                count = 4;
            }
            return Math.Min(count, ThreadLimit);
        }

    }
}
