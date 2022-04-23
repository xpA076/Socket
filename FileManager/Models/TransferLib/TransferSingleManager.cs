using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FileManager.Events;
using FileManager.Events.UI;
using FileManager.Models.Serializable;
using FileManager.Models.TransferLib.Enums;
using FileManager.Models.TransferLib.Info;
using FileManager.Models.TransferLib.Services;
using FileManager.SocketLib;
using FileManager.SocketLib.Enums;
using FileManager.Static;
using FileManager.ViewModels.PageTransfer;

namespace FileManager.Models.TransferLib
{
    /// <summary>
    /// 处理单个 TransferInfoRoot, 调度安排其传输任务
    /// 每个 Dispatcher 对应一个 TransferInfoRoot, 在 Pages 中被事件调用
    /// 每个 TransferInfoRoot 对应的目录树在传输过程中, 同时只能有一个文件处于正在传输状态
    /// </summary>
    public partial class TransferSingleManager
    {
        public TransferInfoRoot RootInfo;

        public TransferThreadPool TransferThreadPool;

        public PageTransferViewModel ViewModel;

        /// <summary>
        /// 这个 Flag 指示当前 TransferManager 是否处于传输状态
        /// </summary>
        public bool IsTransfering { get; private set; } = false;

        /// <summary>
        /// UI 调用传输暂停事件时, 将此 Flag 置为 true
        /// </summary>
        private bool IsPausing = false;

        /// <summary>
        /// 目录路径对应 DFS 缓存栈
        /// </summary>
        private readonly Stack<int> DirectoryIndexStack = new Stack<int>();
        private TransferInfoDirectory CurrentDirectoryInfo = null;
        private int IndexFile = 0;

        public delegate void TransferFinishedEventHandler(object sender, EventArgs e);
        public event TransferFinishedEventHandler TransferFinishedCallback;

        public TransferSingleManager(TransferInfoRoot rootInfo)
        {
            RootInfo = rootInfo;
            CurrentDirectoryInfo = rootInfo;
        }


        public void InitTransfer()
        {
            Task.Run(() => { TransferMain(); });
        }


        private void TransferMain()
        {
            IsTransfering = true;
            /// 线程池和 UI 初始化
            TransferThreadPool.Route = RootInfo.Route;
            TransferThreadPool.InitializeThreads();
            TransferThreadPool.UIFinishBytes += ViewModel.OnFinishBytes;
            ViewModel.StartRefresh();

            /// RootInfo 的文件传输
            if (RootInfo.Type == TransferType.Download)
            {
                ViewModel.TransferStatus = "Querying...";
                /// 确认当前任务已经 Query 完成
                /// 对同一个网络路径的数据通信, 没必要通过Query和文件传输异步来提升效率
                /// 若 Querier 为 null, 说明目录树是从文件或其它方式加载完成, 不需要等待 Query 信号
                if (RootInfo.Querier != null)
                {
                    RootInfo.Querier.QueryCompleteSignal.WaitOne();
                }
                ViewModel.TransferStatus = "Transfering...";
                if (RootInfo.Querier.IsQueryHaveFailed)
                {
                    // todo 若有 Query 被server 拒绝, 可在此处理
                }
                /// --------
                /// 以文件为单位进行主循环, 没必要将目录树序列化, 以块为单位进行主循环
                while (true)
                {
                    /// 将 Stack 和 CurrentDirectoryInfo 指向正确位置
                    if (!MovePointerToFirstFile()) { break; }
                    
                    /// 当前任务传输过程
                    TransferInfoFile infoFile = CurrentDirectoryInfo.FileChildren[IndexFile];
                    infoFile.Status = TransferStatus.Transfering;
                    ViewModel.SetNewFile(infoFile);
                    TransferThreadPool.DownloadOne(infoFile);
                    if (IsPausing)
                    {
                        // todo 保存进度
                        
                        break;
                    }
                    /// 标记当前 File 任务完成
                    CurrentDirectoryInfo.TransferCompleteFileFlags[IndexFile] = true;
                    if (infoFile.Status == TransferStatus.Failed)
                    {
                        ViewModel.CurrentFileFailed();
                    }
                    else
                    {
                        infoFile.Status = TransferStatus.Finished;
                        ViewModel.CurrentFileFinished();
                    }
                }
            }

            /// 传输结束的 UI 显示
            ViewModel.StopRefresh();
            if (IsPausing)
            {
                ViewModel.TransferStatus = "Paused";
                IsPausing = false;
            }
            else
            {
                ViewModel.TransferStatus = "Finished";
            }

            /// 线程池清理
            TransferThreadPool.Finish();
            TransferThreadPool.UIFinishBytes -= ViewModel.OnFinishBytes;
            IsTransfering = false;

            /// 回调
            TransferFinishedCallback(this, EventArgs.Empty);
        }


        /// <summary>
        /// 外部 UI 调用, 暂停下载
        /// </summary>
        public void Pause()
        {
            IsPausing = true;
            /// 执行TransferThreadPool.Pause() 后, 主循环中的 DownloadOne() 函数会返回
            TransferThreadPool.Pause();
        }



        /// <summary>
        /// 从 CurrentDirectoryInfo 开始, DFS查找下一个未传输文件位置
        /// 查找路径上经过的未建立目录的路径, 会建立对应目录
        /// 并将路径上所有目录上节点 TransferInfoDirectory 正确标注
        /// </summary>
        /// <returns>是否成功移动至下一个文件节点, 若返回 false 证明已经全部传输完成</returns>
        private bool MovePointerToFirstFile()
        {
            /// 因为保证整个目录树 Query 完成才能开始传输
            /// 因此调用本函数时, 当前节点的子目录树应该已构建完成, 不考虑目录未构建问题
            while (true)
            {
                /// 按顺序尝试进入当前 Directory 的未完成子目录, 若成功则在子目录重复该循环
                for (int i = 0; i < CurrentDirectoryInfo.DirectoryChildren.Count; ++i)
                {
                    if (!CurrentDirectoryInfo.TransferCompleteDirectoryFlags[i])
                    {
                        DirectoryIndexStack.Push(i);
                        CurrentDirectoryInfo = CurrentDirectoryInfo.DirectoryChildren[i];
                        /// 若成功进入当前子节点, 先建立文件夹, 而后在子目录中重复循环
                        if (!Directory.Exists(CurrentDirectoryInfo.LocalPath))
                        {
                            Directory.CreateDirectory(CurrentDirectoryInfo.LocalPath);
                        }
                        continue;
                    }
                }
                /// 未成功进入子目录, 则尝试获取子节点中的未完成文件
                for (int i = 0; i < CurrentDirectoryInfo.FileChildren.Count; ++i)
                {
                    if (!CurrentDirectoryInfo.TransferCompleteFileFlags[i])
                    {
                        IndexFile = i;
                        return true;
                    }
                }
                /// 未获取到本级中的未完成文件
                ///  if    当前节点为 Root, 说明已经完成所有文件
                ///  else  回溯至上级, 将上级中本节点标记为完成, 在上级中重复查找 File 节点
                CurrentDirectoryInfo.Status = TransferStatus.Finished;
                if (CurrentDirectoryInfo.IsRoot)
                {
                    /// 已回溯至 Root 节点, 所有文件已经完成
                    return false;
                }
                int idx = DirectoryIndexStack.Pop();
                CurrentDirectoryInfo.Parent.TransferCompleteDirectoryFlags[idx] = true;
                CurrentDirectoryInfo = CurrentDirectoryInfo.Parent;
            }
        }





    }
}
