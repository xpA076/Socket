using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SocketFileManager.SocketLib;

namespace SocketFileManager.Models
{
    public class TransferRecord
    {
        #region 属性与变量
        public long updateLengthThres = 1 * 1024 * 1024; // 刷新界面最小字节数
        public int updateTimeThres = 500; // 刷新界面最短时间间隔

        // 这里要用 ObservableCollection 不能用 List
        // 实现引用不变内容改变下的实时显示
        public ObservableCollection<FileTask> FileTasks = new ObservableCollection<FileTask>();
        public int CurrentTaskIndex = 0; // CurrentTaskIndex 一直指向当前未完成的第一个任务

        private long taskAddup = 0; // 当前任务以前任务总byte数
        public long TotalLength { get; set; } = 0;
        public long TotalFinished { get { return taskAddup + CurrentFinished; } }
        public long CurrentLength { get; private set; } = 0;
        public long CurrentFinished { get { return (LastPackage + 1) * HB32Encoding.DataSize; } }

        public int LastPackage = -1;
        public int TotalPackage = 0;

        private DateTime tic = DateTime.Now; // 上次刷新界面时间
        private long newBytes = 0; // 上次刷新界面后传输字节总数
        #endregion

        public FileTask CurrentTask
        {
            get
            {
                return FileTasks[CurrentTaskIndex];
            }
        }

        public bool IsFinished()
        {
            return CurrentTaskIndex >= FileTasks.Count;
        }

        /// <summary>
        /// 启动新任务时调用
        /// </summary>
        public void RecordNewTask()
        {
            taskAddup += CurrentLength; // 将上次任务 byte 数加入 taskAddup
            CurrentLength = CurrentTask.Length; // 更新 CurrentLength
        }


        public void AddFinishedBytes(long count)
        {
            newBytes += count;
        }

        public bool AllowUpdate()
        {
            // 间隔 500ms 以上且传输字节数大于 1M
            return (DateTime.Now - tic).Milliseconds > updateTimeThres && newBytes >= updateLengthThres;
        }
        /// <summary>
        /// 获取上次界面刷新到现在的速度，清除byte累计 并 更新时间戳为现在
        /// </summary>
        /// <returns> 字节传输速度(byte/s) </returns>
        public double GetSpeed()
        {
            DateTime tok = DateTime.Now;
            int ms = (tok - tic).Milliseconds;
            long bytes = newBytes;
            tic = tok;
            newBytes = 0;
            return (double)bytes * 1000 / ms;
        }

        private bool isNeedClear = false;

        public void AddTask(FileTask task)
        {
            if (isNeedClear)
            {
                FileTasks.Clear();
                isNeedClear = false;
            }
            FileTasks.Add(task);
        }

        public void Clear()
        {
            isNeedClear = true;
            CurrentTaskIndex = 0;
            LastPackage = -1;
            TotalPackage = 0;
            taskAddup = 0;
            TotalLength = 0;
            CurrentLength = 0;
            newBytes = 0;
        }

        public void Save()
        {

        }

        public void Load()
        {

        }
    }
}
