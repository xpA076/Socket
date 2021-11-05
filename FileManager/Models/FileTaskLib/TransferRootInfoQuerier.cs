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

namespace FileManager.Models
{
    public class TransferRootInfoQuerier
    {
        private TransferRootInfo rootInfo = null;

        private List<DownloadConfirmViewModel> downloadConfirmViewModels = null;


        public TransferRootInfoQuerier(TransferRootInfo rootInfo)
        {
            this.rootInfo = rootInfo;
        }




        private bool StopQueryFlag = false;




        public void StartQuery()
        {
            if (rootInfo.Type == TransferType.Download)
            {
                Task.Run(() => { DownloadQuery(); });
            }
        }

        private void DownloadQuery()
        {
            while (!rootInfo.IsQueryComplete)
            {
                if (StopQueryFlag)
                {
                    break;
                }
                TransferDirectoryInfo currentInfo = GetFirstQueryInfo();
                try
                {
                    HB32Response resp = SocketFactory.RequestWithHeaderFlag(SocketPacketFlag.DirectoryResponse,
                        new HB32Header(SocketPacketFlag.DirectoryRequest), 
                        Encoding.UTF8.GetBytes(currentInfo.RemotePath));
                    List<SocketFileInfo> respInfos = SocketFileInfo.BytesToList(resp.Bytes);
                    currentInfo.BuildChildrenFrom(respInfos);
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
                                pt = pt.Parent;
                            }
                            else
                            {
                                break;
                            }
                        }
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
                if (currentInfo.Parent.IsRoot)
                {
                    if (this.downloadConfirmViewModels != null)
                    {
                        DownloadConfirmViewModel viewModel = GetDownloadConfirmViewModel(currentInfo);
                        viewModel.SetLength(currentInfo.Length);
                    }
                }
            }
        }

        public void StopQuery()
        {
            StopQueryFlag = true;
        }



        private TransferDirectoryInfo GetFirstQueryInfo()
        {
            if (rootInfo.IsQueryComplete)
            {
                return null;
            }
            TransferDirectoryInfo dirInfo = rootInfo;
            while (dirInfo.IsChildrenListBuilt)
            {
                dirInfo = dirInfo.DirectoryChildren[dirInfo.QueryCompleteCount];
            }
            return dirInfo;
        }


        private TransferDirectoryInfo GetNextQueryInfo(TransferDirectoryInfo currentInfo)
        {
            /// DFS 回溯
            return null;
        }

        public List<DownloadConfirmViewModel> LinkDownloadConfirmViewModels()
        {
            this.downloadConfirmViewModels = new List<DownloadConfirmViewModel>();
            foreach (TransferDirectoryInfo directoryInfo in this.rootInfo.DirectoryChildren)
            {
                downloadConfirmViewModels.Add(new DownloadConfirmViewModel(directoryInfo));
            }
            foreach (TransferFileInfo fileInfo in this.rootInfo.FileChildren)
            {
                downloadConfirmViewModels.Add(new DownloadConfirmViewModel(fileInfo));
            }
            return this.downloadConfirmViewModels;
        }

        private DownloadConfirmViewModel GetDownloadConfirmViewModel(TransferDirectoryInfo dirInfo)
        {
            foreach (DownloadConfirmViewModel viewModel in this.downloadConfirmViewModels)
            {
                if (viewModel.Name == dirInfo.Name)
                {
                    return viewModel;
                }
            }
            return null;
        }

        public void UnlinkDownloadConfirmViewModels()
        {
            this.downloadConfirmViewModels = null;
        }

    }
}
