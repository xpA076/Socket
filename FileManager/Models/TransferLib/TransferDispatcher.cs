using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FileManager.SocketLib;
using FileManager.SocketLib.Enums;
using FileManager.Static;

namespace FileManager.Models.TransferLib
{
    /// <summary>
    /// 处理单个 TransferInfoRoot, 调度安排其传输任务
    /// 每个 Dispatcher 对应一个 TransferInfoRoot, 在 Pages 中被事件调用
    /// </summary>
    public partial class TransferDispatcher
    {

        #region Properties

        private TransferInfoRoot RootInfo;

        private TransferTaskDispatcher TaskDispatcher;

        private bool IsTransfering = false;



        #endregion

        public TransferDispatcher(TransferInfoRoot rootInfo)
        {
            RootInfo = rootInfo;
            TaskDispatcher = new TransferTaskDispatcher(rootInfo);
        }

        public void InitTransfer()
        {
            Task.Run(() => { TransferMain(); });
        }

        private void TransferMain()
        {
            IsTransfering = true;



            IsTransfering = false;
        }


        private bool SetTransferSession(string path, int id, TransferType transferType)
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
            SocketClient client = null;
            client = SocketFactory.GenerateConnectedSocketClient();


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
            if (client != null)
            {
                client.Close();
            }

        }




        private bool IsTransferComplete()
        {
            // todo

            return false;
        }

    }
}
