using FileManager.Exceptions;
using FileManager.SocketLib;
using FileManager.SocketLib.Enums;
using FileManager.Static;
using FileManager.ViewModels;
using FileManager.Models.Serializable;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FileManager.Models.TransferLib.Info;
using FileManager.Models.TransferLib.Enums;

namespace FileManager.Models.TransferLib.Services
{
    public class TransferInfoRootQuerier
    {
        private readonly TransferInfoRoot RootInfo = null;

        private List<DownloadConfirmViewModel> DownloadConfirmViewModels = null;

        /// <summary>
        /// 通过将此 Flag 置为 True 停止当前 Query 任务
        /// </summary>
        private bool IsStopQuery = false;

        /// <summary>
        /// 若Query过程中有子目录被Server拒绝, 将此 Flag 会被置为true
        /// </summary>
        public bool IsQueryHaveFailed { get; private set; } = false;


        /// <summary>
        /// 此 Flag 表示目前是否正在运行 Query
        /// </summary>
        public bool IsQuerying { get; private set; } = false;

        public readonly ManualResetEvent QueryCompleteSignal = new ManualResetEvent(false);


        private readonly Stack<int> QueryIndexStack = new Stack<int>();

        private TransferInfoDirectory CurrentDirectoryInfo = null;


        public void StartQuery()
        {
            if (RootInfo.Type == TransferType.Download)
            {
                Task.Run(() => { DownloadQuery(); });
            }
        }

        public TransferInfoRootQuerier(TransferInfoRoot rootInfo)
        {
            this.RootInfo = rootInfo;
        }



        /// <summary>
        /// DFS 方式遍历并建立目录树
        /// </summary>
        private void DownloadQuery()
        {
            IsQuerying = true;
            QueryCompleteSignal.Reset();
            while (true)
            {
                if (IsStopQuery)
                {
                    break;
                }
                if (CurrentDirectoryInfo == null)
                {
                    /// 首次进入循环, 从根节点开始查询
                    CurrentDirectoryInfo = RootInfo;
                    QueryIndexStack.Clear();
                    if (this.RootInfo.IsQueryComplete)
                    {
                        /// 任务只有FileChildren, 不必再 Query 目录
                        TryCompleteParent();
                        QueryCompleteSignal.Set();
                        break;
                    }
                    else
                    {
                        /// 任务中包含未建立过的子目录节点, 指针移动至首个未建立的子目录
                        while (CurrentDirectoryInfo.IsChildrenListBuilt)
                        {
                            MoveToFirstUnqueriedChild();
                        }
                    }
                }
                else
                {
                    /// 非首次进入循环, 指针移动至首个未完成的子目录节点
                    MoveToFirstUnqueriedChild();
                }
                try
                {
                    /// Query 并建立当前子节点
                    DirectoryRequest request = new DirectoryRequest(CurrentDirectoryInfo.RemotePath);
                    HB32Response hb_resp = SocketFactory.Instance.Request(HB32Packet.DirectoryRequest, request.ToBytes());
                    DirectoryResponse response = DirectoryResponse.FromBytes(hb_resp.Bytes);
                    if (response.Type != DirectoryResponse.ResponseType.ListResponse)
                    {
                        throw new SocketTypeException(response.ExceptionMessage);
                    }
                    CurrentDirectoryInfo.BuildChildrenFrom(response.FileInfos);
                    //Thread.Sleep(200);
                    /// 回溯并完成父节点, 直至父节点包含未完成子节点
                    if (TryCompleteParent())
                    {
                        /// 若 Query 完成, 在这里退出循环
                        QueryCompleteSignal.Set();
                        break;
                    }
                }
                catch (SocketTypeException ex)
                {
                    /// 当前任务标记为失败
                    IsQueryHaveFailed = true;
                    Logger.Log(ex.Message, LogLevel.Warn);
                    Thread.Sleep(1000);
                    continue;
                }
                catch (Exception)
                {
                    /// socket 异常
                    Thread.Sleep(1000);
                    continue;
                }
            }
            IsQuerying = false;
        }


        private void MoveToFirstUnqueriedChild()
        {
            int idx;
            for (idx = 0; idx < CurrentDirectoryInfo.DirectoryChildren.Count; ++idx)
            {
                if (!CurrentDirectoryInfo.QueryCompleteFlags[idx])
                {
                    break;
                }
            }
            QueryIndexStack.Push(idx);
            CurrentDirectoryInfo = CurrentDirectoryInfo.DirectoryChildren[idx];
        }


        /// <summary>
        /// 在向 Server 请求构建当前节点后调用, CurrentInfo 会停在DFS回溯后
        ///  -> 首个具有未完成子节点的位置
        /// </summary>
        /// <returns>是否构建完成整个目录树</returns>
        private bool TryCompleteParent()
        {
            /// 当前节点非叶子节点, 下一个循环会进入当前节点的子节点
            ///  -> 无需计算Length, 直接返回
            if (CurrentDirectoryInfo.DirectoryChildren.Count > 0)
            {
                return false;
            }
            while (true)
            {
                /// 当前节点无子节点 (首次进入循环)
                /// 或当前节点的全部子Directory节点已Query完成 (通过continue重复循环)
                CurrentDirectoryInfo.Status = TransferStatus.Waiting;
                CurrentDirectoryInfo.CalculateLength();
                /// 函数退出条件
                if (CurrentDirectoryInfo.IsRoot)
                {
                    return true;
                }
                /// 更新 ViewModel
                TryUpdateViewModels(CurrentDirectoryInfo);
                /// 回溯至上级
                int child_idx = QueryIndexStack.Pop(); 
                CurrentDirectoryInfo = CurrentDirectoryInfo.Parent;
                CurrentDirectoryInfo.QueryCompleteFlags[child_idx] = true;
                /// 循环退出条件
                if (CurrentDirectoryInfo.IsQueryComplete)
                {
                    continue;
                }
                else
                {
                    return false;
                }
            }
        }



        public void StopQuery()
        {
            IsStopQuery = true;
        }


        /// <summary>
        /// 尝试更新 DownloadConfirmWindow 中的 ViewModel (文件长度信息)
        /// 若无关联的 ViewModel 会直接跳过
        /// </summary>
        /// <param name="info"></param>
        private void TryUpdateViewModels(TransferInfoDirectory info)
        {
            if (!info.Parent.IsRoot) { return; }
            if (this.DownloadConfirmViewModels != null)
            {
                foreach (DownloadConfirmViewModel viewModel in this.DownloadConfirmViewModels)
                {
                    if (viewModel.Name == info.Name)
                    {
                        viewModel.SetLength(info.Length);
                    }
                }
            }
        }


        /// <summary>
        /// 将 Querier 与对应 ViewModel 关联, 在 DownloadConfirmWindow 中调用
        /// </summary>
        /// <returns></returns>
        public List<DownloadConfirmViewModel> LinkDownloadConfirmViewModels()
        {
            this.DownloadConfirmViewModels = new List<DownloadConfirmViewModel>();
            foreach (TransferInfoDirectory directoryInfo in this.RootInfo.DirectoryChildren)
            {
                DownloadConfirmViewModels.Add(new DownloadConfirmViewModel(directoryInfo));
            }
            foreach (TransferInfoFile fileInfo in this.RootInfo.FileChildren)
            {
                DownloadConfirmViewModels.Add(new DownloadConfirmViewModel(fileInfo));
            }
            return this.DownloadConfirmViewModels;
        }


        /// <summary>
        /// 取消 Querier 与 ViewModel 的关联, 响应 DownloadConfirmWindow 关闭事件
        /// </summary>
        public void UnlinkDownloadConfirmViewModels()
        {
            this.DownloadConfirmViewModels = null;
        }

    }
}
