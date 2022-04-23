﻿using FileManager.Models.TransferLib.Info;
using FileManager.Models.TransferLib.Services;
using FileManager.ViewModels.PageTransfer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.Models.TransferLib
{
    public class TransferManager
    {
        private TransferThreadPool TransferThreadPool = new TransferThreadPool();

        public readonly List<TransferInfoRoot> InfoRoots = new List<TransferInfoRoot>();

        private readonly List<TransferSingleManager> SingleManagers = new List<TransferSingleManager>();

        public PageTransferViewModel ViewModel;

        public bool IsTransfering
        {
            get
            {
                foreach (TransferSingleManager m in SingleManagers)
                {
                    if (m.IsTransfering)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public void AddTransferTask(TransferInfoRoot rootInfo)
        {
            if (this.IsTransfering)
            {
                InfoRoots.Add(rootInfo);
            }
            else
            {
                InfoRoots.Clear();
                SingleManagers.Clear();
                InfoRoots.Add(rootInfo);
                OnTransferManagerFinished(null, null);
            }
        }


        /// <summary>
        /// 若调用方为 null, 则为从 AddTransferTask() 调用, 此时从首个 InfoRoot 开始任务
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnTransferManagerFinished(object sender, EventArgs e)
        {
            int idx;
            if (sender == null)
            {
                idx = 0;
            }
            else
            {
                TransferSingleManager m = sender as TransferSingleManager;
                for (idx = 0; idx < InfoRoots.Count; ++idx)
                {
                    if (m == SingleManagers[idx])
                    {
                        idx++;
                        break;
                    }
                }
            }
            /// 此时 idx 指向首个未完成 InfoRoot 节点索引
            if (idx < InfoRoots.Count)
            {
                TransferSingleManager tm = new TransferSingleManager(InfoRoots[idx]);
                TransferThreadPool.Route = InfoRoots[idx].Route;
                tm.TransferThreadPool = TransferThreadPool;
                tm.ViewModel = ViewModel;
                tm.TransferFinishedCallback += OnTransferManagerFinished;
                ViewModel.SetNewRoot(InfoRoots[idx]);
                SingleManagers.Add(tm);
                tm.InitTransfer();
            }
            else
            {
                /// 全部传输完成, 可在此做后续处理
            }
        }


    }
}
