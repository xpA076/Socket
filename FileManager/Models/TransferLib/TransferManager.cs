using FileManager.Models.Config;
using FileManager.Models.TransferLib.Info;
using FileManager.Models.TransferLib.Services;
using FileManager.Static;
using FileManager.ViewModels.PageTransfer;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FileManager.Models.TransferLib
{
    public class TransferManager
    {
        private ConfigService configService = Program.Provider.GetService<ConfigService>();

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
                ViewModel.AfterAddNewRoot();
            }
            else
            {
                InfoRoots.Clear();
                SingleManagers.Clear();
                InfoRoots.Add(rootInfo);
                ViewModel.AfterAddNewRoot();
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
                ViewModel.SetCurrentRoot(idx);
                SingleManagers.Add(tm);
                tm.InitTransfer();
                if (!IsRecording)
                {
                    StartRecord();
                }
            }
            else
            {
                /// 全部传输完成, 可在此做后续处理
                StopRecord();
                DeleteRecord();
            }
        }

        private bool IsRecording = false;

        private readonly ManualResetEvent StopRecordSignal = new ManualResetEvent(false);
        private readonly ManualResetEvent StopRecordFinishSignal = new ManualResetEvent(false);

        private void StartRecord()
        {
            IsRecording = true;
            StopRecordSignal.Reset();
            StopRecordFinishSignal.Reset();
            Task.Run(() => { RecordCycle(); });
        }


        private void StopRecord()
        {
            StopRecordSignal.Set();
            StopRecordFinishSignal.WaitOne();
        }


        private void DeleteRecord()
        {
            //File.Delete(FileManager.Static.Config.RecordPath);
        }


        private void RecordCycle()
        {
            while (true)
            {
                if (StopRecordSignal.WaitOne(configService.SaveRecordInterval))
                {
                    SaveRecord();
                    IsRecording = false;
                    StopRecordFinishSignal.Set();
                    break;
                }
                else
                {
                    SaveRecord();
                }
            }
        }


        private void SaveRecord()
        {

        }



    }
}
