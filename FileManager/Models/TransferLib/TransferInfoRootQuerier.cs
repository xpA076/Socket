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
        private TransferInfoDirectory CurrentInfo = null;

        private void SetFirstQueryInfo()
        {
            CurrentInfo = RootInfo;
            QueryIndexStack.Clear();
            while (CurrentInfo.IsChildrenListBuilt)
            {
                MoveToFirstUnqueriedChild();
            }
        }

        private void MoveToFirstUnqueriedChild()
        {
            int idx;
            for (idx = 0; idx < CurrentInfo.DirectoryChildren.Count; ++idx)
            {
                if (!CurrentInfo.QueryCompleteFlags[idx])
                {
                    break;
                }
            }
            QueryIndexStack.Push(idx);
            CurrentInfo = CurrentInfo.DirectoryChildren[idx];
        }

        /// <summary>
        /// 在向 Server 请求构建当前节点后调用, CurrentInfo 会停在DFS回溯后
        ///   首个具有未完成子节点的位置
        /// </summary>
        /// <returns>是否构建完成整个目录树</returns>
        private bool TryCompleteParent()
        {
            /// 当前节点非叶子节点则无需计算Length, 直接返回
            if (CurrentInfo.DirectoryChildren.Count > 0)
            {
                return false;
            }
            while (true)
            {
                /// 当前节点处理
                CurrentInfo.CalculateLength();
                if (CurrentInfo.IsRoot)
                {
                    return true;
                }
                if (CurrentInfo.Parent.IsRoot)
                {
                    TryUpdateViewModels(CurrentInfo);
                }
                /// 回溯至上级
                int child_idx = QueryIndexStack.Pop(); 
                CurrentInfo = CurrentInfo.Parent;
                CurrentInfo.QueryCompleteFlags[child_idx] = true;
                /// 循环退出条件
                if (CurrentInfo.IsQueryComplete)
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
                if (CurrentInfo == null)
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
                    HB32Response resp = SocketFactory.RequestWithHeaderFlag(SocketPacketFlag.DirectoryResponse,
                        new HB32Header(SocketPacketFlag.DirectoryRequest), 
                        Encoding.UTF8.GetBytes(CurrentInfo.RemotePath));
                    List<SocketFileInfo> respInfos = SocketFileInfo.BytesToList(resp.Bytes);
                    CurrentInfo.BuildChildrenFrom(respInfos);
                    Thread.Sleep(200);
                    if (TryCompleteParent())
                    {
                        break;
                    }

                    /*
                    /// 判断当前节点是否为叶子节点 (不再包含子目录, 构造完成并计算 Length)
                    if (respInfos.Count == 0 || !respInfos[0].IsDirectory)
                    {
                        currentInfo.CalculateLength();
                        /// 向父节点以及可能更高节点反馈其子节点构造完成
                        TransferDirectoryInfo pt = currentInfo.Parent;
                        while (true)
                        {
                            pt.QueryCompleteCount++;
                            if (pt.IsQueryComplete)
                            {
                                pt.CalculateLength();
                                if (pt.IsRoot)
                                {
                                    break;
                                }
                                else
                                {
                                    /// 更新 ViewModel
                                    if (pt.Parent.IsRoot)
                                    {
                                        TryUpdateViewModels(pt);
                                    }
                                    else
                                    {
                                        ;
                                    }
                                    /// 继续返回上级
                                    pt = pt.Parent;
                                }
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                    */



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
