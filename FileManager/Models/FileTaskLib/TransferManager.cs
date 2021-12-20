using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FileManager.SocketLib.Enums;


namespace FileManager.Models
{
    /// <summary>
    /// 处理单个 TransferRootInfo, 调度安排其传输任务
    /// </summary>
    public class TransferManager
    {
        #region Properties

        private TransferRootInfo RootInfo;

        private readonly Stack<int> TaskIndexStack = new Stack<int>();

        private bool IsTransfering = false;

        #endregion

        public TransferManager(TransferRootInfo rootInfo)
        {
            RootInfo = rootInfo;
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




        private bool IsTransferComplete()
        {
            // todo

            return false;
        }

    }
}
