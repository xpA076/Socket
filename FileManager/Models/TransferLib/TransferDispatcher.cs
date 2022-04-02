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



        private readonly Stack<int> DirectoryIndexStack = new Stack<int>();
        private TransferInfoDirectory CurentDirectoryInfo = null;
        private int FileIndex = 0;
        private bool IsFirstMovePointer = true;

        #endregion

        public TransferDispatcher(TransferInfoRoot rootInfo)
        {
            RootInfo = rootInfo;
            CurentDirectoryInfo = rootInfo;
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
            /// todo 在这里确认当前任务已经 Query 完成
            /// 先不搞异步那些, 对同一个路径的数据通信, 没必要通过异步提升效率
            /// /todo

            /// --------
            /// 以文件为单位进行主循环
            while (true)
            {
                /// 将 Stack 和 CurrentDirectoryInfo 指向正确位置
                if (!MovePointerToFirstFile()) { break; }


            }
        }



        private bool MovePointerToFirstFile()
        {
            if (IsFirstMovePointer)
            {
                /// 获取第一个文件
                //CurentDirectoryInfo = RootInfo;


            }
            else
            {
                /// 获取下一个文件
                while (true)
                {
                    /// 创建当前目录
                    if (!Directory.Exists(CurentDirectoryInfo.LocalPath))
                    {
                        Directory.CreateDirectory(CurentDirectoryInfo.LocalPath);
                    }
                    /// 按顺序尝试进入当前 Directory 的未完成子目录, 若成功则在子目录重复该循环
                    int c_dirs = CurentDirectoryInfo.DirectoryChildren.Count;
                    for (int i = 0; i < c_dirs; ++i)
                    {
                        if (!CurentDirectoryInfo.TransferCompleteFlags[i])
                        {
                            DirectoryIndexStack.Push(i);
                            CurentDirectoryInfo = CurentDirectoryInfo.DirectoryChildren[i];
                            continue;
                        }
                    }
                    /// 未成功进入子目录, 则尝试获取子节点中的未完成文件
                    for (int i = 0; i < CurentDirectoryInfo.FileChildren.Count; ++i)
                    {
                        if (!CurentDirectoryInfo.TransferCompleteFlags[i + c_dirs])
                        {
                            FileIndex = i;
                            return true;
                        }
                    }
                    /// 未获取到本级中的未完成文件, 回溯至上级




                }




            }





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
