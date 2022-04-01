using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FileManager.SocketLib;
using FileManager.SocketLib.Enums;
using FileManager.Static;

namespace FileManager.Models.TransferLib
{
    /// <summary>
    /// 处理单个 TransferInfoRoot, 调度安排其传输任务
    /// 每个 Dispatcher 对应一个 TransferInfoRoot, 在 Pages 中被事件调用
    /// 每个 TransferInfoRoot 对应的目录树在传输过程中, 同时只能有一个文件处于正在传输状态
    /// </summary>
    public partial class TransferDispatcher
    {

        #region Properties

        private TransferInfoRoot RootInfo;

        /// <summary>
        /// 这个一会删掉
        /// </summary>
        private TransferTaskDispatcher TaskDispatcher;

        private bool IsTransfering = false;



        private readonly Stack<int> TaskIndexPointer = new Stack<int>();
        private TransferInfoDirectory CurentDirectoryInfo = null;

        #endregion

        public TransferDispatcher(TransferInfoRoot rootInfo)
        {
            RootInfo = rootInfo;
            //CurentDirectoryInfo = rootInfo;
            TaskDispatcher = new TransferTaskDispatcher(rootInfo);
        }

        public void InitTransfer()
        {
            Task.Run(() => { TransferMain(); });
        }

        private void TransferMain()
        {
            IsTransfering = true;

            if (RootInfo.Type == TransferType.Download)
            {
                DownloadMain();
            }


            IsTransfering = false;
        }

        private void DownloadMain()
        {
            while (true)
            {
                if (!MovePointerToFirstFile()) { break; }
                if (CurentDirectoryInfo == null)
                {
                    /// 获取第一个文件
                    CurentDirectoryInfo = RootInfo;
                    while (true)
                    {
                        if (!Directory.Exists(CurentDirectoryInfo.LocalPath))
                        {
                            Directory.CreateDirectory(CurentDirectoryInfo.LocalPath);
                        }


                    }


                }
                else
                {

                }




            }
        }



        private bool MovePointerToFirstFile()
        {



        }


        //private ReaderWriterLockSlim _lockStack = new ReaderWriterLockSlim();



        private bool SetTransferSession(string path, TransferType transferType)
        {
            

            return false;
        }

        
        private void RunSubThreads()
        {

        }


        private void TransferThreadUnit()
        {

            

        }

        private void DownloadThreadUnit()
        {
            for (TransferTaskBlock block = TaskDispatcher.GetNewTask(); block != null;
                block = TaskDispatcher.GetNewTask())
            {
                // 根据 block 内容完成任务
                switch (block.Content)
                {
                    case TransferTaskBlock.BlockContent.SetSession:
                        


                        break;
                    case TransferTaskBlock.BlockContent.Transfer:

                        break;
                }

                // 
                //TaskDispatcher.FinishTask(block, TransferTaskBlock.BlockStatus.Success);

            }
        }




        private bool IsTransferComplete()
        {
            // todo

            return false;
        }

    }
}
