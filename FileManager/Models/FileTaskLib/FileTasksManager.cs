using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using FileManager.Static;
using FileManager.SocketLib;
using FileManager.SocketLib.Enums;
using FileManager.Events;
using FileManager.Exceptions;

namespace FileManager.Models
{
    /// <summary>
    /// 文件传输管理类 
    /// 负责 执行, 监控, 管理文件传输任务
    /// </summary>
    public class FileTasksManager
    {
        #region 属性与变量
        public event UpdateUIEventHandler UpdateUI;
        public event UpdateUIEventHandler UpdateProgress;
        public event UpdateUIInvokeEventHandler UpdateTasklist;

        public bool IsTransfering { get; private set; } = false;
        public bool IsStopDownloading { get; set; } = false;

        private FileTaskRecord Record = new FileTaskRecord();

        public long CurrentFinished { get { return Record.CurrentFinished; } }
        public long CurrentLength { get { return Record.CurrentLength; } }
        public long TotalFinished { get { return Record.TotalFinished; } }
        public long TotalLength { get { return Record.TotalLength; } }

        /// <summary>
        /// PageTransfer 中 ListView 内容对应此 FileTasks 列表, 直接引用 FileTaskRecord 中的 FileTasks
        /// 对于 FileTasks 的所有 CRUD 操作都必须经过 FileTaskRecord 中暴露的接口而不应直接操作列表以保证线程安全
        /// </summary>
        public ObservableCollection<FileTask> FileTasks { get { return Record.FileTasks; } }

        private readonly object PacketLock = new object();
        private readonly HashSet<int> TransferingPackets = new HashSet<int>();
        private readonly HashSet<int> FinishedPackets = new HashSet<int>();


        private Thread TransferMainThread;
        private Thread[] TransferSubThreads;

        private DateTime tic = DateTime.Now;

        /// <summary>
        /// 在上次 tic 之后传输的 bytes 数量 
        /// </summary>
        private long TicTokBytes { get; set; } = 0;

        private FileStream localFileStream = null;

        #endregion

        public void Load()
        {
            lock (this.Record)
            {
                Record = new FileTaskRecord();
                Record.LoadXml();
            }
        }



        public bool AllowUpdateUI()
        {
            return (DateTime.Now - tic).TotalMilliseconds >= Config.UpdateTimeThreshold 
                && TicTokBytes >= Config.UpdateLengthThreshold;
        }


        /// <summary>
        /// 获取上次界面刷新到现在的速度，清除byte累计 并 更新时间戳为现在
        /// </summary>
        /// <returns> 字节传输速度(byte/s) </returns>
        public double GetSpeed()
        {
            DateTime tok = DateTime.Now;
            int ms = (tok - tic).Milliseconds;
            long bytes = TicTokBytes;
            tic = tok;
            TicTokBytes = 0;
            return (double)bytes * 1000 / ms;
        }


        /// <summary>
        /// 响应 Pages 中的添加任务调用
        ///   对于Directory 任务会获取任务文件夹大小
        /// </summary>
        /// <param name="task"></param>
        public void AddTask(FileTask task)
        {
            UpdateTaskLength(task);
            Record.AddTask(task);
        }


        #region Task length

        /// <summary>
        /// 对于即将添加到 FileTasks 列表中的 directory 任务, 应获取其总大小
        /// </summary>
        /// <param name="task"></param>
        private void UpdateTaskLength(FileTask task)
        {
            if (task.Type == TransferType.Download)
            {
                if (task.IsDirectory && task.Length == 0)
                {
                    task.Length = GetDownloadDirectoryTaskLength(task);
                }
            }
            else
            {
                if (task.IsDirectory && task.Length == 0)
                {
                    task.Length = GetLocalDirectoryLength(task.LocalPath);
                }

            }
        }


        /// <summary>
        /// 下载FileTask加入队列前, 对文件夹任务获取总大小
        /// </summary>
        /// <param name="task"></param>
        /// <returns></returns>
        public static long GetDownloadDirectoryTaskLength(FileTask task)
        {
            try
            {
                var resp = SocketFactory.Request(SocketPacketFlag.DirectorySizeRequest, Encoding.UTF8.GetBytes(task.RemotePath));
                SocketEndPoint.CheckFlag(SocketPacketFlag.DirectorySizeResponse, resp);
                return long.Parse(Encoding.UTF8.GetString(resp.Bytes));
            }
            catch (Exception)
            {
                return -1;
            }
        }


        /// <summary>
        /// 在 UplaodTask 中, 获取目录文件大小
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static long GetLocalDirectoryLength(string path)
        {
            long size = 0;
            DirectoryInfo dir = new DirectoryInfo(path);
            FileInfo[] fileInfos = dir.GetFiles();
            foreach (FileInfo fileInfo in fileInfos)
            {
                if (true)
                {
                    size += fileInfo.Length;
                }
            }
            DirectoryInfo[] directoryInfos = dir.GetDirectories();
            foreach (DirectoryInfo directoryInfo in directoryInfos)
            {
                if (true)
                {
                    size += GetLocalDirectoryLength(directoryInfo.FullName);
                }
            }
            return size;
        }

        #endregion


        public void InitDownload()
        {
            TransferMainThread = new Thread(TransferMain);
            TransferMainThread.IsBackground = true;
            TransferMainThread.Start();
        }


        /// <summary>
        /// 阻塞直至所有下载子线程结束
        /// </summary>
        /// 
        public void Pause()
        {
            lock (this.Record)
            {
                Record.SaveXml();
                TransferingPackets.Clear();
                FinishedPackets.Clear();
            }
            IsStopDownloading = true;
        }


        /// <summary>
        /// 下载的主线程，循环下载列表中每一个任务直至所有任务完成
        /// </summary>
        private void TransferMain()
        {
            IsTransfering = true;
            // 直到 currentTaskIndex 指向最后，代表所有任务完成
            while (!Record.IsFinished())
            {
                FileTask current_task = Record.CurrentTask;
                if (current_task.IsDirectory)
                {
                    Record.StartNewTask(current_task);
                    TransferDirectoryTask(current_task);
                }
                else
                {
                    UpdateUI(this, EventArgs.Empty);
                    Record.StartNewTask(current_task);
                    TransferSingleTask(current_task);
                    if (IsStopDownloading)
                    {
                        IsStopDownloading = false;
                        IsTransfering = false;
                        return;
                    }
                    Record.FinishCurrentTask();
                    Record.CurrentTaskIndex++;
                }
            }
            /// 界面更新 100%
            TicTokBytes = 0;
            this.Record.CurrentFinished = this.Record.CurrentLength;
            this.Record.Clear();
            UpdateProgress(this, EventArgs.Empty);
            IsTransfering = false;
        }


        #region Transfer directory

        /// <summary>
        /// 启动 Directory 传输任务, 更新Task列表后按原 Record.CurrentIndex 返回
        /// </summary>
        /// <param name="task"></param>
        private void TransferDirectoryTask(FileTask task)
        {
            if (task.Type == TransferType.Upload) 
            { 
                UploadSingleTaskDirectory(task); 
            }
            else 
            {
                DownloadSingleTaskDirectory(task); 
            }
        }


        private void DownloadSingleTaskDirectory(FileTask task)
        {
            /// 创建本地 directory
            if (!Directory.Exists(task.LocalPath))
            {
                DirectoryInfo dirInfo = new DirectoryInfo(task.LocalPath);
                dirInfo.Create();
            }
            /// 获取 server 端文件列表
            List<SocketFileInfo> files;
            try
            {
                SocketClient client = SocketFactory.GenerateConnectedSocketClient(task, 1);
                client.SendBytes(SocketPacketFlag.DirectoryRequest, task.RemotePath);
                client.ReceiveBytesWithHeaderFlag(SocketPacketFlag.DirectoryResponse, out byte[] recv_bytes);
                files = SocketFileInfo.BytesToList(recv_bytes);
                client.Close();
            }
            catch (Exception)
            {
                task.Status = FileTaskStatus.Denied;
                return;
            }
            /// 更新 TaskList, 重新定位当前任务
            UpdateTasklist(this,
                new UpdateUIInvokeEventArgs(new Action(() => {
                    this.Record.RemoveTaskAt(Record.CurrentTaskIndex);
                    int bias = Record.CurrentTaskIndex;
                    for (int i = 0; i < files.Count; ++i)
                    {
                        SocketFileInfo f = files[i];
                        FileTask task_add = new FileTask
                        {
                            Route = task.Route.Copy(),
                            IsDirectory = f.IsDirectory,
                            Type = TransferType.Download,
                            RemotePath = Path.Combine(task.RemotePath, f.Name),
                            LocalPath = task.LocalPath + "\\" + f.Name,
                            Length = f.Length,
                        };
                        UpdateTaskLength(task_add);
                        this.Record.InsertTask(bias + i, task_add);
                    }
                })));
        }

        private void UploadSingleTaskDirectory(FileTask task)
        {
            /// 请求 server 端创建目录
            try
            {
                SocketClient client = SocketFactory.GenerateConnectedSocketClient(task, 1);
                int pt = 0;
                byte[] headerBytes = BytesConverter.WriteString(new byte[4], task.RemotePath, ref pt);
                client.SendBytes(SocketPacketFlag.CreateDirectoryRequest, headerBytes);
                client.ReceiveBytesWithHeaderFlag(SocketPacketFlag.CreateDirectoryAllowed, out byte[] recvBytes);
                client.Close();
            }
            catch (Exception)
            {
                task.Status = FileTaskStatus.Denied;
                return;
            }
            /// 更新 TaskList, 重新定位当前任务
            UpdateTasklist(this,
                new UpdateUIInvokeEventArgs(new Action(() => {
                    lock (this.Record)
                    {
                        FileTask father_task = Record.CurrentTask;
                        int bias = Record.CurrentTaskIndex;
                        this.Record.RemoveTaskAt(Record.CurrentTaskIndex);
                        List<FileTask> tasks_son = GetUploadTasksInDirectory(father_task);
                        for (int i = 0; i < tasks_son.Count; ++i)
                        {
                            FileTask task_son = tasks_son[i];
                            UpdateTaskLength(task_son);
                            this.Record.InsertTask(bias + i, task_son);
                        }
                    }
                })));
        }


        /// <summary>
        /// 在 Uplaod Directory时, 获取 local_path 下所有子 FileTask 列表
        /// </summary>
        /// <param name="local_path"></param>
        /// <param name="remote_path"></param>
        /// <returns></returns>
        private List<FileTask> GetUploadTasksInDirectory(FileTask father_task)
        {
            string local_path = father_task.LocalPath;
            string remote_path = father_task.RemotePath;
            DirectoryInfo directory = new DirectoryInfo(local_path);
            DirectoryInfo[] directoryInfos = directory.GetDirectories();
            List<FileTask> tasks = new List<FileTask>();
            foreach (DirectoryInfo dir_info in directoryInfos)
            {
                tasks.Add(new FileTask
                {
                    Route = father_task.Route.Copy(),
                    IsDirectory = true,
                    Type = TransferType.Upload,
                    RemotePath = remote_path + "\\" + dir_info.Name,
                    LocalPath = local_path + "\\" + dir_info.Name,
                    Length = 0,
                });
            }
            FileInfo[] fileInfos = directory.GetFiles();
            foreach (FileInfo file_info in fileInfos)
            {
                tasks.Add(new FileTask
                {
                    Route = father_task.Route.Copy(),
                    IsDirectory = false,
                    Type = TransferType.Upload,
                    RemotePath = remote_path + "\\" + file_info.Name,
                    LocalPath = local_path + "\\" + file_info.Name,
                    Length = file_info.Length,
                });
            }
            tasks.Sort(FileTask.Compare);
            return tasks;
        }


        #endregion

        /// <summary>
        /// 启动 File 传输任务，根据当前 FileTask 任务区分传输模式，并阻塞直到所有子线程完成后,
        ///    将 currentTaskIndex 指向下个 task，返回
        /// 当前任务为 小文件 则启用单线程传输
        /// 当前任务为 大文件 则启用多线程传输
        /// </summary>
        private void TransferSingleTask(FileTask task)
        {
            Logger.Log(string.Format("<FileTaskManager> call TransferSingleTask, {0, 20}{1}", "", task.ToString()), LogLevel.Debug);
            if (task.Type == TransferType.Download)
            {
                task.Status = FileTaskStatus.Downloading;
            }
            else if (task.Type == TransferType.Upload)
            {
                task.Status = FileTaskStatus.Uploading;
            }
            if (task.Length <= Config.SmallFileThreshold)
            {
                TransferSingleTaskSmallFile(task, task.Type);
            }
            else
            {
                TransferSingleTaskBigFile(task);
            }
        }


        private void TransferSingleTaskSmallFile(FileTask task, TransferType taskType)
        {
            try
            {
                SocketClient client = SocketFactory.GenerateConnectedSocketClient(task, 1);
                if (taskType == TransferType.Upload)
                {
                    int pt = 0;
                    byte[] headerBytes = BytesConverter.WriteString(new byte[4], task.RemotePath, ref pt);
                    byte[] contentBytes = File.ReadAllBytes(task.LocalPath);
                    byte[] bytes = new byte[headerBytes.Length + contentBytes.Length];
                    Array.Copy(headerBytes, 0, bytes, 0, headerBytes.Length);
                    Array.Copy(contentBytes, 0, bytes, headerBytes.Length, contentBytes.Length);
                    client.SendBytes(SocketPacketFlag.UploadRequest, bytes);
                    client.ReceiveBytesWithHeaderFlag(SocketPacketFlag.UploadAllowed, out byte[] recvBytes);
                    client.Close();
                }
                else
                {
                    client.SendBytes(SocketPacketFlag.DownloadRequest, task.RemotePath);
                    client.ReceiveBytesWithHeaderFlag(SocketPacketFlag.DownloadAllowed, out byte[] bytes);
                    client.Close();
                    File.WriteAllBytes(task.LocalPath, bytes);
                }
                this.Record.CurrentFinished = task.Length;
                task.Status = FileTaskStatus.Success;
            }
            catch (Exception ex)
            {
                task.Status = FileTaskStatus.Failed;
                System.Windows.Forms.MessageBox.Show(ex.Message);
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="task"></param>
        private void TransferSingleTaskBigFile(FileTask task)
        {
            FileTaskDispatcher dispatcher = new FileTaskDispatcher(task);

            /// 请求 server 端打开文件, 并获取 FileStreamId
            dispatcher.RequestFileStreamId();
            if (dispatcher.FileStreamId == -1) 
            {
                task.Status = FileTaskStatus.Failed;
                Record.CurrentTaskIndex++;
                return; 
            }


            /// 创建本地 FileStream
            localFileStream = new FileStream(task.LocalPath, FileMode.OpenOrCreate,
                task.Type == TransferType.Upload ? FileAccess.Read : FileAccess.Write);


            /// 运行下载子线程
            FileTaskStatus result = RunTransferSubThreads(dispatcher);

            /// 结束 Transfer, 关闭 FileStream
            localFileStream.Close();
            localFileStream = null;

            /// 请求server端关闭并释放文件
            try
            {
                SocketClient sc = SocketFactory.GenerateConnectedSocketClient(dispatcher.Task.Route, 1);
                if (task.Type == TransferType.Upload)
                {
                    sc.SendBytes(SocketPacketFlag.UploadPacketRequest, new byte[1], dispatcher.FileStreamId, -1);
                }
                else
                {
                    sc.SendHeader(SocketPacketFlag.DownloadPacketRequest, dispatcher.FileStreamId, -1);
                }
                sc.Close();
            }
            catch (Exception ex)
            {
                Logger.Log("Transfer finished. But server FileStream not correctly closed. " + ex.Message, LogLevel.Warn);
            }

            /// 判断从子线程退出返回原因是否是 Pause
            if (IsStopDownloading)
            {
                task.Status = FileTaskStatus.Pause;
            }
            else
            {
                task.Status = result;
            }
        }





        private FileTaskStatus RunTransferSubThreads(FileTaskDispatcher dispatcher)
        {
            dispatcher.IsCurrentTaskFailed = false;
            /// 清空 packets 缓存记录
            lock (this.PacketLock)
            {
                TransferingPackets.Clear();
                FinishedPackets.Clear();
            }
            int threads_count = Config.ThreadLimit;
            TransferSubThreads = new Thread[threads_count];
            for (int i = 0; i < threads_count; ++i)
            {
                if (dispatcher.Task.Type == TransferType.Upload)
                {
                    TransferSubThreads[i] = new Thread(new ParameterizedThreadStart(UploadThreadUnit)) { IsBackground = true };
                }
                else
                {
                    TransferSubThreads[i] = new Thread(new ParameterizedThreadStart(DownloadThreadUnit)) { IsBackground = true };
                }
                TransferSubThreads[i].Start(dispatcher);
                Thread.Sleep(50);
            }
            /// 阻塞至子线程工作完毕
            for (int i = 0; i < threads_count; ++i)
            {
                TransferSubThreads[i].Join();
            }
            /// 确定 task 状态
            if (dispatcher.IsCurrentTaskFailed)
            {
                dispatcher.IsCurrentTaskFailed = false;
                return FileTaskStatus.Failed;
            }
            else
            {
                return FileTaskStatus.Success;
            }
        }



        private void DownloadThreadUnit(object o)
        {
            FileTaskDispatcher dispatcher = (FileTaskDispatcher)o;
            SocketClient client = null;
            int packet = -1;
            while (true)
            {
                /// 当前循环对应请求的 packet index
                packet = dispatcher.GeneratePacket();
                /// while 循环退出条件
                if (packet == -1)
                {
                    /// 已无需要传输的 packet
                    break;
                }
                if (IsStopDownloading || dispatcher.IsCurrentTaskFailed)
                {
                    /// 点击 Pause 按钮, 或其它线程触发了 dispatcher.IsCurrentTaskFailed
                    break;
                }
                /// 首次循环, 或 Socket 连接异常等情况下建立 Socket 连接
                if (client == null)
                {
                    while (!IsStopDownloading)
                    {
                        try
                        {
                            client = SocketFactory.GenerateConnectedSocketClient(dispatcher.Task.Route, 1);
                            break;
                        }
                        catch
                        {
                            continue;
                        }
                    }
                }
                HB32Header recv_header;
                byte[] recv_bytes;
                /// 通过 fsid 向 server 请求 bytes, 成功后离开 try 块 (包含在 server 重启后重新获取 fsid 的异常处理)
                try
                {
                    /// 向 server 发送请求
                    if (dispatcher.IsRequestingFsid)
                    {
                        /// 此时其它线程正在获取fsid并已获得 FsidLock 锁
                        /// 等待其它线程释放 obj_lock_request_fsid 后, 可通过 fsid 请求packet
                        lock (dispatcher.FsidLock)
                        {
                            client.SendHeader(SocketPacketFlag.DownloadPacketRequest, dispatcher.FileStreamId, packet);
                        }
                    }
                    else
                    {
                        /// 正常请求packet
                        client.SendHeader(SocketPacketFlag.DownloadPacketRequest, dispatcher.FileStreamId, packet);
                    }
                    /// 接收 server 响应
                    client.ReceiveBytesWithHeaderFlag(SocketPacketFlag.DownloadPacketResponse, out recv_header, out recv_bytes);
                }
                /// DownloadDenied 异常处理
                catch (SocketFlagException ex)
                {
                    /// 释放已申请的 packet
                    dispatcher.ReleasePacket(packet);
                    if (ex.Header.I1 > 0)
                    {
                        /// ↓对应情况: Server 重启后没有对应 fsid, client端原有 fsid 不可用
                        if (dispatcher.IsRequestingFsid)
                        {
                            /// 此时其它子线程正在获取 fsid
                            /// 执行 continue 后进入 lock(obj_lock_request_fsid) 等待获取fsid的线程释放锁
                            ;
                        }
                        else
                        {
                            /// 加锁获取 fsid, 此时其它子线程等待当前线程获取 fsid 后再发包
                            dispatcher.IsRequestingFsid = true;
                            lock (dispatcher.FsidLock)
                            {
                                dispatcher.RequestFileStreamId();
                                dispatcher.IsRequestingFsid = false;
                            }
                        }
                        continue;
                    }
                    else
                    {
                        /// task failed, 置 flag 后退出子线程
                        dispatcher.IsCurrentTaskFailed = true;
                        break;
                    }
                }
                catch (Exception)
                {
                    /// 释放已申请的 packet
                    dispatcher.ReleasePacket(packet);
                    /// 将 SocketClient 置为 null, 在新循环开始时重建 SocketClient
                    client = null;
                    continue;
                }
                /// 请求数据成功, 写入本地文件
                lock (this.localFileStream)
                {
                    localFileStream.Seek((long)packet * HB32Encoding.DataSize, SeekOrigin.Begin);
                    localFileStream.Write(recv_bytes, 0, recv_header.ValidByteLength);
                }
                /// 后处理: HashSet完成当前块, 更新 Record
                dispatcher.FinishPacket(packet);
                lock (this.Record)
                {
                    TicTokBytes += recv_header.ValidByteLength;
                    Record.CurrentFinished += recv_header.ValidByteLength;
                    // todo 更新UI的判定放在dispatcher里 21.07.12
                    if (this.AllowUpdateUI())
                    {
                        UpdateUI(this, EventArgs.Empty);
                        if (Record.NeedSaveRecord())
                        {
                            //dispatcher.Task.FinishedPacket = dispatcher.FinishedPacket;
                            Record.SaveXml();
                        }
                    }
                }
            }
            if (client != null)
            {
                client.Close();
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="o"></param>
        private void UploadThreadUnit(object o)
        {
            return;
            /*
            FileTask task = (FileTask)o;
            SocketClient client = SocketFactory.GenerateConnectedSocketClient(task);
            int packet = GeneratePacketIndex(task);
            while (packet != -1)
            {
                if (IsStopDownloading || IsCurrentTaskFailed)
                {
                    break;
                }
                try
                {
                    /// 根据 packet 读取 FileStream 中 bytes
                    long begin = (long)packet * HB32Encoding.DataSize;
                    int length = HB32Encoding.DataSize;
                    if(begin + HB32Encoding.DataSize > task.Length)
                    {
                        length = (int)(task.Length - begin);
                    }
                    byte[] contentBytes = new byte[length];
                    lock (this.localFileStream)
                    {
                        localFileStream.Seek((long)packet * HB32Encoding.DataSize, SeekOrigin.Begin);
                        localFileStream.Read(contentBytes, 0, length);
                    }
                    /// 上传 packet 对应 bytes
                    if (IsRequestingFsid)
                    {
                        lock (obj_lock_request_fsid)
                        {
                            client.SendBytes(SocketPacketFlag.UploadPacketRequest, contentBytes, task.FileStreamId, packet);
                        }
                    }
                    else
                    {
                        client.SendBytes(SocketPacketFlag.UploadPacketRequest, contentBytes, task.FileStreamId, packet);
                    }
                    client.ReceiveBytes(out HB32Header header, out byte[] bytes);
                    /// 异常处理
                    if (header.Flag == SocketPacketFlag.UploadDenied)
                    {
                        if (header.I1 > 0)
                        {
                            if (IsRequestingFsid) { continue; }
                            else
                            {
                                IsRequestingFsid = true;
                                lock (obj_lock_request_fsid)
                                {
                                    task.FileStreamId = GetFileStreamId(task);
                                    IsRequestingFsid = false;
                                }
                            }
                        }
                        else
                        {
                            IsCurrentTaskFailed = true;
                            break;
                        }
                    }
                    /// 完成当前 packet
                    FinishPacket(task, packet);
                    lock (this.Record)
                    {
                        TicTokBytes += length;
                        Record.CurrentFinished += length;
                        if (this.AllowUpdateUI())
                        {
                            UpdateUI(this, EventArgs.Empty);
                            // Save record xml
                            Record.SaveXml();
                        }
                    }
                    packet = GeneratePacketIndex(task);
                }
                catch (Exception)
                {
                    while (!IsStopDownloading)
                    {
                        try
                        {
                            client = SocketFactory.GenerateConnectedSocketClient(task, 1);
                            break;
                        }
                        catch
                        {
                            continue;
                        }
                    }
                }
            }
            client.Close();
            */
        }





    }
}
