using FileManager.Exceptions;
using FileManager.SocketLib;
using FileManager.SocketLib.Enums;
using FileManager.Static;
using FileManager.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FileManager.Models.TransferLib
{
    public class TransferInfoRootQuerier
    {
        private TransferInfoRoot RootInfo = null;

        private List<DownloadConfirmViewModel> DownloadConfirmViewModels = null;


        public TransferInfoRootQuerier(TransferInfoRoot rootInfo)
        {
            this.RootInfo = rootInfo;
        }




        private bool StopQueryFlag = false;




        public void StartQuery()
        {
            if (RootInfo.Type == TransferType.Download)
            {
                Task.Run(() => { DownloadQuery(); });
            }
        }

        private readonly Stack<int> QueryIndexStack = new Stack<int>();
        private TransferInfoDirectory CurrentDirectoryInfo = null;

        private void SetFirstQueryInfo()
        {
            CurrentDirectoryInfo = RootInfo;
            QueryIndexStack.Clear();
            while (CurrentDirectoryInfo.IsChildrenListBuilt)
            {
                MoveToFirstUnqueriedChild();
            }
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
                /// 当前节点处理
                CurrentDirectoryInfo.CalculateLength();
                if (CurrentDirectoryInfo.IsRoot)
                {
                    return true;
                }
                if (CurrentDirectoryInfo.Parent.IsRoot)
                {
                    TryUpdateViewModels(CurrentDirectoryInfo);
                }
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

        /// <summary>
        /// DFS 方式遍历并建立目录树
        /// Debugged at 2021.11.19
        /// </summary>
        private void DownloadQuery()
        {
            
            while (true)
            {
                if (StopQueryFlag)
                {
                    break;
                }
                //TransferDirectoryInfo currentInfo = __GetFirstQueryInfo();
                if (CurrentDirectoryInfo == null)
                {
                    SetFirstQueryInfo();
                }
                else
                {
                    MoveToFirstUnqueriedChild();
                }
                try
                {
                    /// Query 并建立当前子节点
                    HB32Response resp = SocketFactory.Instance.RequestWithHeaderFlag(SocketPacketFlag.DirectoryResponse,
                        new HB32Header(SocketPacketFlag.DirectoryRequest), 
                        Encoding.UTF8.GetBytes(CurrentDirectoryInfo.RemotePath));
                    List<SocketFileInfo> respInfos = SocketFileInfo.BytesToList(resp.Bytes);
                    CurrentDirectoryInfo.BuildChildrenFrom(respInfos);
                    Thread.Sleep(200);
                    if (TryCompleteParent())
                    {
                        break;
                    }
                }
                catch (SocketFlagException ex)
                {
                    // todo something (当前任务标记为失败)
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
                /// 更新 ViewModel
                /*
                if (currentInfo.Parent.IsRoot)
                {
                    if (this.downloadConfirmViewModels != null)
                    {
                        DownloadConfirmViewModel viewModel = GetDownloadConfirmViewModel(currentInfo);
                        viewModel.SetLength(currentInfo.Length);
                    }
                }
                */
            }
        }

        public void StopQuery()
        {
            StopQueryFlag = true;
        }



        #region ViewModels in DownloadConfirmWindow
        /// <summary>
        /// 尝试更新 DownloadConfirmWindow 中的 ViewModel (文件长度信息)
        /// 若无关联的 ViewModel 会直接跳过
        /// </summary>
        /// <param name="info"></param>
        private void TryUpdateViewModels(TransferInfoDirectory info)
        {
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


        public void UnlinkDownloadConfirmViewModels()
        {
            this.DownloadConfirmViewModels = null;
        }
        #endregion

    }
}
