using SocketLib;
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

namespace FileManager.Models
{
    /// <summary>
    /// 文件传输管理类 
    /// 负责 执行, 监控, 管理文件传输任务
    /// </summary>
    public class FileTaskManager
    {
        #region 属性与变量
        public PageUICallback UpdateUICallback { private get; set; }
        public PageUICallback UpdateProgressCallback { private get; set; }
        public PageUIInvokeCallback UpdateTasklistCallback { private get; set; }

        public bool IsTransfering { get; private set; } = false;
        public bool StopDownloading { get; set; } = false;

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

        #region Pages 对应列表操作

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

        #endregion

        /// <summary>
        /// 对于即将添加到 FileTasks 列表中的 directory 任务, 应获取其总大小
        /// </summary>
        /// <param name="task"></param>
        private void UpdateTaskLength(FileTask task)
        {
            if (task.IsDirectory && task.Length == 0)
            {
                task.Length = GetDirectoryTaskLength(task);
            }
        }

        /// <summary>
        /// FileTask加入队列前, 对文件夹任务获取总大小
        /// </summary>
        /// <param name="task"></param>
        /// <returns></returns>
        public static long GetDirectoryTaskLength(FileTask task)
        {
            try
            {
                SocketClient client = SocketFactory.GenerateConnectedSocketClient(task.TcpAddress);
                client.SendBytes(SocketDataFlag.DirectorySizeRequest, task.RemotePath);
                client.ReceiveBytes(client.client, out _, out byte[] bytes);
                return long.Parse(Encoding.UTF8.GetString(bytes));
            }
            catch (Exception)
            {
                return -1;
            }
        }



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
            StopDownloading = true;
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
                // *** to do *** 这里有 bug
                // 对于后添加的 dir 任务, 其CurrentLength不能正确计算为零
                // 21.04.17 10:05
                






                // *** todo *** 先这样写着, 应该把directory 和 file 的传输逻辑分开 if-else
                bool current_is_dir = Record.CurrentTask.IsDirectory;
                Record.StartNewTask();
                // 界面更新当前任务
                if (!current_is_dir) { UpdateUICallback(); }
                // 启动下载
                TransferSingleTask(Record.CurrentTask);
                if (StopDownloading)
                {
                    StopDownloading = false;
                    IsTransfering = false;
                    return;
                }
                /// 完成下载
                Record.FinishCurrentTask(current_is_dir);
                if (!current_is_dir) { Record.CurrentTaskIndex++; }
            }
            /// 界面更新 100%
            TicTokBytes = 0;
            this.Record.CurrentFinished = this.Record.CurrentLength;
            this.Record.Clear();
            UpdateProgressCallback();
            IsTransfering = false;
        }



        /// <summary>
        /// 启动传输任务，根据当前 FileTask 任务区分传输模式，并阻塞直到所有子线程完成后,
        ///    将 currentTaskIndex 指向下个 task，返回
        /// 当前任务为 directory 则展开 directory ，更新 fileTasks 后返回
        /// 当前任务为 小文件 则启用单线程传输
        /// 当前任务为 大文件 则启用多线程传输
        /// </summary>
        private void TransferSingleTask(FileTask task)
        {
            Logger.Log(string.Format("<FileTaskManager> call TransferSingleTask, {0, 20}{1}", "", task.ToString()), LogLevel.Debug);

            if (task.IsDirectory)
            {
                if (task.Type == FileTaskType.Upload) { UploadSingleTaskDirectory(task); }
                else { DownloadSingleTaskDirectory(task); }
                return;
            }
            if (task.Type == FileTaskType.Download)
            {
                task.Status = FileTaskStatus.Downloading;
            }
            else if (task.Type == FileTaskType.Download)
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


        private void DownloadSingleTaskDirectory(FileTask task)
        {
            /// 创建本地 directory
            if (!Directory.Exists(task.LocalPath))
            {
                DirectoryInfo dirInfo = new DirectoryInfo(task.LocalPath);
                dirInfo.Create();
            }
            /// 获取 server 端文件列表
            SocketFileInfo[] files;
            try
            {
                SocketClient client = SocketFactory.GenerateConnectedSocketClient(task, 1);
                files = client.RequestDirectory(task.RemotePath);
                client.Close();
            }
            catch (Exception)
            {
                task.Status = FileTaskStatus.Denied;
                return;
            }
            /// 更新 TaskList, 重新定位当前任务
            UpdateTasklistCallback(new Action(() => {
                this.Record.RemoveTaskAt(Record.CurrentTaskIndex);
                int bias = Record.CurrentTaskIndex;
                for (int i = 0; i < files.Length; ++i)
                {
                    SocketFileInfo f = files[i];
                    FileTask task_add = new FileTask
                    {
                        TcpAddress = task.TcpAddress.Copy(),
                        IsDirectory = f.IsDirectory,
                        Type = FileTaskType.Download,
                        RemotePath = Path.Combine(task.RemotePath, f.Name),
                        LocalPath = task.LocalPath + "\\" + f.Name,
                        Length = f.Length,
                    };
                    UpdateTaskLength(task_add);
                    this.Record.InsertTask(bias + i, task_add);
                }
            }));
        }

        private void UploadSingleTaskDirectory(FileTask task)
        {
            /// 请求 server 端创建目录
            try
            {
                SocketClient client = SocketFactory.GenerateConnectedSocketClient(task, 1);
                int keyLength = 16;
                byte[] keyBytes = GenerateKeyBytes(keyLength);
                byte[] headerBytes = BytesParser.WriteString(keyBytes, task.RemotePath, ref keyLength);
                client.SendBytes(SocketDataFlag.CreateDirectoryRequest, headerBytes);
                client.ReceiveBytes(client.client, out HB32Header header, out byte[] recvBytes);
                client.Close();
                if (header.Flag != SocketDataFlag.CreateDirectoryAllowed)
                {
                    throw new Exception(Encoding.UTF8.GetString(recvBytes));
                }
            }
            catch (Exception)
            {
                task.Status = FileTaskStatus.Denied;
                return;
            }
            /// 更新 TaskList, 重新定位当前任务
            UpdateTasklistCallback(new Action(() => {
                lock (this.Record)
                {
                    this.Record.FileTasks.RemoveAt(Record.CurrentTaskIndex);
                    DirectoryInfo directory = new DirectoryInfo(task.LocalPath);
                    FileInfo[] fileInfos = directory.GetFiles();
                    DirectoryInfo[] directoryInfos = directory.GetDirectories();
                    /// 添加 task
                    List<FileTask> tasks = new List<FileTask>();
                    int bias = Record.CurrentTaskIndex;

                    // *********************** todo *********
                    // UploadSingleTaskDirectory insert 时没有考虑文件夹大小 2021.02.07
                    // **************************

                    for (int i = 0; i < directoryInfos.Length; ++i)
                    {
                        DirectoryInfo d = directoryInfos[i];
                        this.Record.FileTasks.Insert(bias + i, new FileTask
                        {
                            IsDirectory = true,
                            Type = FileTaskType.Upload,
                            RemotePath = task.RemotePath + "\\" + d.Name,
                            LocalPath = task.LocalPath + "\\" + d.Name,
                            Length = 0,
                        });
                        //Record.TotalLength += 0;
                    }
                    for (int i = 0; i < fileInfos.Length; ++i)
                    {
                        FileInfo f = fileInfos[i];
                        this.Record.FileTasks.Insert(bias + i, new FileTask
                        {
                            IsDirectory = false,
                            Type = FileTaskType.Upload,
                            RemotePath = task.RemotePath + "\\" + f.Name,
                            LocalPath = task.LocalPath + "\\" + f.Name,
                            Length = f.Length,
                        });
                        //Record.TotalLength += f.Length;
                    }
                }
            }));
        }


        private void TransferSingleTaskSmallFile(FileTask task, FileTaskType taskType)
        {
            try
            {
                SocketClient client = SocketFactory.GenerateConnectedSocketClient(task, 1);
                if (taskType == FileTaskType.Upload)
                {
                    int keyLength = 16;
                    byte[] keyBytes = GenerateKeyBytes(keyLength);
                    byte[] headerBytes = BytesParser.WriteString(keyBytes, task.RemotePath, ref keyLength);
                    byte[] contentBytes = File.ReadAllBytes(task.LocalPath);
                    byte[] bytes = new byte[headerBytes.Length + contentBytes.Length];
                    Array.Copy(headerBytes, 0, bytes, 0, headerBytes.Length);
                    Array.Copy(contentBytes, 0, bytes, headerBytes.Length, contentBytes.Length);
                    client.SendBytes(SocketDataFlag.UploadRequest, bytes);
                    client.ReceiveBytes(client.client, out HB32Header header, out byte[] recvBytes);
                    client.Close();
                    if (header.Flag != SocketDataFlag.UploadAllowed)
                    {
                        throw new Exception(Encoding.UTF8.GetString(recvBytes));
                    }
                }
                else
                {
                    client.SendBytes(SocketDataFlag.DownloadRequest, task.RemotePath);
                    client.ReceiveBytes(client.client, out HB32Header header, out byte[] bytes);
                    client.Close();
                    if (header.Flag != SocketDataFlag.DownloadAllowed)
                    {
                        throw new Exception(Encoding.UTF8.GetString(bytes));
                    }
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


        private void TransferSingleTaskBigFile(FileTask task)
        {

            /// 获取 FileStreamId
            task.FileStreamId = GetFileStreamId(task);
            if (task.FileStreamId == -1) 
            {
                task.Status = FileTaskStatus.Failed;
                Record.CurrentTaskIndex++;
                return; 
            }


            /// 创建本地 FileStream
            localFileStream = new FileStream(task.LocalPath, FileMode.OpenOrCreate,
                task.Type == FileTaskType.Upload ? FileAccess.Read : FileAccess.Write);


            /// 运行下载子线程
            RunTransferSubThreads(task);

            /// 结束 Transfer, 关闭 FileStream
            localFileStream.Close();
            localFileStream = null;

            /// 请求server端关闭并释放文件
            try
            {
                SocketClient sc = SocketFactory.GenerateConnectedSocketClient(task, 1);
                if (task.Type == FileTaskType.Upload)
                {
                    sc.SendBytes(SocketDataFlag.UploadPacketRequest, new byte[1], task.FileStreamId, -1);
                }
                else
                {
                    sc.SendHeader(sc.client, SocketDataFlag.DownloadPacketRequest, task.FileStreamId, -1);
                }
                sc.Close();
            }
            catch (Exception ex)
            {
                Logger.Log("Transfer finished. But server FileStream not correctly closed. " + ex.Message, LogLevel.Warn);
            }

            /// 判断从子线程退出返回原因是否是 Pause
            if (StopDownloading)
            {
                task.Status = FileTaskStatus.Pause;
            }
            else
            {
                task.Status = FileTaskStatus.Success;
            }
        }

        /// <summary>
        /// 根据 FileTask 向 Server 端请求 FileStreamId (0~65535), 如有异常返回-1
        /// </summary>
        /// <param name="task"></param>
        /// <returns></returns>
        private int GetFileStreamId (FileTask task)
        {
            SocketDataFlag mask = (SocketDataFlag)((task.Type == FileTaskType.Upload ? 1 : 0) << 8);
            try
            {
                SocketClient client = SocketFactory.GenerateConnectedSocketClient(task, 1);
                if (task.Type == FileTaskType.Upload)
                {
                    int keyLength = 16;
                    byte[] keyBytes = GenerateKeyBytes(keyLength);
                    byte[] headerBytes = BytesParser.WriteString(keyBytes, task.RemotePath, ref keyLength);
                    client.SendBytes(SocketDataFlag.UploadFileStreamIdRequest, headerBytes);
                }
                else
                {
                    client.SendBytes(SocketDataFlag.DownloadFileStreamIdRequest, task.RemotePath);
                }
                client.ReceiveBytes(client.client, out HB32Header header, out byte[] bytes);
                client.Close();
                if (header.Flag != (SocketDataFlag.DownloadAllowed ^ mask))
                {
                    throw new Exception(Encoding.UTF8.GetString(bytes));
                }
                string response = Encoding.UTF8.GetString(bytes);
                return int.Parse(response);
            }
            catch (Exception ex)
            {
                Logger.Log("Cannot get FileStreamID, Exception : " + ex.Message);
                System.Windows.Forms.MessageBox.Show(ex.Message);
                return -1;
            }

        }



        private void RunTransferSubThreads(FileTask task)
        {
            /// 清空 packets 缓存记录
            lock (this.PacketLock)
            {
                TransferingPackets.Clear();
                FinishedPackets.Clear();
            }
            TransferSubThreads = new Thread[Config.ThreadLimit];
            for (int i = 0; i < Config.ThreadLimit; ++i)
            {
                if (task.Type == FileTaskType.Upload)
                {
                    TransferSubThreads[i] = new Thread(new ParameterizedThreadStart(UploadThreadUnit)) { IsBackground = true };
                }
                else
                {
                    TransferSubThreads[i] = new Thread(new ParameterizedThreadStart(DownloadThreadUnit)) { IsBackground = true };
                }
                TransferSubThreads[i].Start(task);
                Thread.Sleep(50);
            }
            Config.ThreadLimit = 2;
            /// 阻塞至子线程工作完毕
            for (int i = 0; i < Config.ThreadLimit; ++i)
            {
                TransferSubThreads[i].Join();
            }
        }


        /// <summary>
        /// Server 端重启后, 原有 fsid 不可用, 在某一 SubThread 中首先重新调用 GetFileStreamId()
        /// 将此 flag 置为 true
        /// 此时其余线程执行 SendHeader() 或 SendBytes() 应加锁, 保证在获取 fsid 后再发包
        /// </summary>
        private bool FlagRequestingFsid { get; set; } = false;

        private readonly object LockRequestFsid = new object();

        private void DownloadThreadUnit(object o)
        {
            FileTask task = (FileTask)o;
            SocketClient client = SocketFactory.GenerateConnectedSocketClient(task, -1);
            int packet = GeneratePacketIndex(task);
            while (packet != -1)
            {
                if (StopDownloading)
                {
                    break;
                }
                try
                {
                    if (FlagRequestingFsid)
                    {
                        lock (LockRequestFsid)
                        {
                            client.SendHeader(client.client, SocketDataFlag.DownloadPacketRequest, task.FileStreamId, packet);
                        }
                    }
                    else
                    {
                        client.SendHeader(client.client, SocketDataFlag.DownloadPacketRequest, task.FileStreamId, packet);
                    }
                    client.ReceivePacket(client.client, out HB32Header header, out byte[] bytes);
                    /// ↓对应情况: Server 重启后没有对应 fsid, client端原有 fsid 不可用
                    if (header.Flag == SocketDataFlag.DownloadDenied)
                    {
                        
                        if (FlagRequestingFsid)
                        {
                            /// 此时其它子线程正在获取 fsid
                            /// 执行 continue 后进入 LockRequestFsid 等待获取fsid的线程释放锁
                            continue;
                        }
                        else
                        {
                            /// 加锁获取 fsid, 此时其它子线程等待当前线程获取 fsid 后再发包
                            FlagRequestingFsid = true;
                            lock (LockRequestFsid)
                            {
                                task.FileStreamId = GetFileStreamId(task);
                                FlagRequestingFsid = false;
                            }
                        }
                    }
                    lock (this.localFileStream)
                    {
                        localFileStream.Seek((long)packet * HB32Encoding.DataSize, SeekOrigin.Begin);
                        localFileStream.Write(bytes, 0, header.ValidByteLength);
                    }
                    FinishPacket(task, packet);
                    lock (this.Record)
                    {
                        TicTokBytes += header.ValidByteLength;
                        Record.CurrentFinished += header.ValidByteLength;
                        if (this.AllowUpdateUI())
                        {
                            UpdateUICallback();
                            if (Record.NeedSaveRecord())
                            {
                                Record.SaveXml();
                            }
                        }
                    }
                    packet = GeneratePacketIndex(task);
                }
                catch (Exception)
                {
                    /// 生成已连接 SocketClient
                    while (!StopDownloading)
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
        }


        private void UploadThreadUnit(object o)
        {
            FileTask task = (FileTask)o;
            int fsid = task.FileStreamId;
            SocketClient client = SocketFactory.GenerateConnectedSocketClient(task);
            int packet = GeneratePacketIndex(task);
            while (packet != -1)
            {
                if (StopDownloading)
                {
                    break;
                }
                try
                {
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
                    client.SendBytes(SocketDataFlag.UploadPacketRequest, contentBytes, fsid, packet);
                    client.ReceiveBytes(client.client, out HB32Header header, out byte[] bytes);
                    FinishPacket(task, packet);
                    lock (this.Record)
                    {
                        TicTokBytes += length;
                        Record.CurrentFinished += length;
                        if (this.AllowUpdateUI())
                        {
                            UpdateUICallback();
                            // Save record xml
                            Record.SaveXml();
                        }
                    }
                    packet = GeneratePacketIndex(task);
                }
                catch (Exception)
                { 
                    client = SocketFactory.GenerateConnectedSocketClient(task);
                }
            }
            client.Close();
        }



        /// <summary>
        /// 申请获取任务packet index, 任务完成则返回 -1
        /// 根据 packet 数目更新 UI
        /// </summary>
        /// <param name="task"></param>
        /// <returns> packet index </returns>
        private int GeneratePacketIndex(FileTask task)
        {
            lock (this.PacketLock)
            {
                int packet = task.FinishedPacket;
                while (packet < task.TotalPacket)
                {
                    if (TransferingPackets.Contains(packet) || FinishedPackets.Contains(packet))
                    {
                        packet++;
                        continue;
                    }
                    else
                    {
                        TransferingPackets.Add(packet);
                        //Log.ThreadPackage(Thread.CurrentThread.ManagedThreadId, package, "generate", task.FinishedPackage.ToString());
                        return packet;
                    }
                }
                return -1;
            }
        }


        /// <summary>
        /// packet 完成写入后清除记录并修正完成package数目
        /// </summary>
        /// <param name="task"></param>
        /// <param name="packet">完成写入的packet index</param>
        private void FinishPacket(FileTask task, int packet)
        {
            lock (this.PacketLock)
            {
                if (TransferingPackets.Contains(packet))
                {
                    TransferingPackets.Remove(packet);
                    FinishedPackets.Add(packet);
                }
                while (FinishedPackets.Contains(task.FinishedPacket))
                {
                    FinishedPackets.Remove(task.FinishedPacket);
                    task.FinishedPacket++;
                }
            }
        }



        private byte[] GenerateKeyBytes(int keyLength = 16)
        {
            return new byte[keyLength];
        }


    }
}
