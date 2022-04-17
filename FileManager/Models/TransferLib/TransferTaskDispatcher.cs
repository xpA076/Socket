using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FileManager.Models.TransferLib
{
    public class TransferTaskDispatcher
    {
        private class CurrentTaskInfo
        {
            public readonly HashSet<TransferTaskBlock> TransferingTasks = new HashSet<TransferTaskBlock>();
            public int Id = -1;
            public int Count = 0;

            public bool IsSessionBuilt
            {
                get
                {
                    return false;
                }
            }
        }


        private TransferInfoRoot RootInfo;

        private readonly Stack<int> TaskIndexStack = new Stack<int>();

        private ReaderWriterLockSlim _lockStack = new ReaderWriterLockSlim();

        private readonly object _lockTask = new object();

        private readonly HashSet<string> EmergentTasks = new HashSet<string>();

        private CurrentTaskInfo CurrentTask = new CurrentTaskInfo();


        //private readonly HashSet<TransferTaskBlock> FinishedTasks = new HashSet<TransferTaskBlock>();


        private bool IsBuildingSession = false;

        private int GenerateId()
        {
            Random rd = new Random();
            int id = rd.Next(0, 65535);
            while (id == CurrentTask.Id)
            {
                id = rd.Next(0, 65535);
            }
            return id;
        }

        private int GetCurrentId()
        {
            return 0;
        }

        public TransferTaskDispatcher(TransferInfoRoot rootInfo)
        {
            RootInfo = rootInfo;
        }

        public void BuildStack()
        {

        }

        private int GetIndexByCurrentId()
        {
            //todo
            return 0;
        }


        public TransferTaskBlock GetNewTask()
        {
            //todo
            return null;
            try
            {
                Monitor.Enter(CurrentTask);

            }
            catch (Exception)
            {
                Monitor.Exit(CurrentTask);
            }
            lock (CurrentTask)
            {

            }

            /*
            TransferTaskBlock block = GetNewTaskMain();
            while (block == null)
            {
                block = GetNewTaskMain();
            }
            return block;
            */
        }

        private TransferTaskBlock GetNewTaskMain()
        {

            return null;
            _lockStack.EnterUpgradeableReadLock();

            // 先拿到ReadLock, 有index就返回 若无index
            //    互斥锁拿到WriteLock 
            //        拿不到 -- null
            //        拿到 -- SetSession 结束后释放WriteLock
            //
        }


        public void FinishTask(TransferTaskBlock block, TransferTaskBlock.BlockStatus status)
        {
            if (status == TransferTaskBlock.BlockStatus.Success)
            {
                switch (block.Content)
                {
                    case TransferTaskBlock.BlockContent.SetSession:
                        FinishTask_SetSession(block);
                        break;
                    case TransferTaskBlock.BlockContent.Transfer:
                        FinishTask_Transfer(block);
                        break;
                }
            }
            else
            {
                // exception, do sth
            }
        }

        private void FinishTask_SetSession(TransferTaskBlock block)
        {

        }

        private void FinishTask_Transfer(TransferTaskBlock block)
        {

        }



        public void SetEmergentTask(string path)
        {

        }

        public void RemoveEmergentTask(string path)
        {

        }
    }

    public partial class TransferDispatcher
    {


    }
}
