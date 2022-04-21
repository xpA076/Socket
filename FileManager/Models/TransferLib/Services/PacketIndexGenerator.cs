using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.Models.TransferLib.Services
{
    public class PacketIndexGenerator
    {
        private readonly HashSet<long> WorkingIndexes = new HashSet<long>();

        private readonly HashSet<long> FinishedIndexes = new HashSet<long>();

        private object IndexLock = new object();

        public long LastFinishedIndex { get; set; }

        public long TotalIndex { get; set; }


        public void Clear()
        {
            lock (IndexLock)
            {
                WorkingIndexes.Clear();
                FinishedIndexes.Clear();
                LastFinishedIndex = 0;
                TotalIndex = 0;
            }
        }

        /// <summary>
        /// 按顺序申请下一个 Index, 若全部分配完则返回 -1
        /// </summary>
        /// <returns></returns>
        public long GenerateIndex()
        {
            lock (IndexLock)
            {
                long idx = LastFinishedIndex;
                while (idx < TotalIndex)
                {
                    if (WorkingIndexes.Contains(idx) || FinishedIndexes.Contains(idx))
                    {
                        idx++;
                        continue;
                    }
                    else
                    {
                        WorkingIndexes.Add(idx);
                        return idx;
                    }
                }
            }
            return -1;
        }

        /// <summary>
        /// Index 对应任务完成后更新记录
        /// </summary>
        /// <param name="index"></param>
        public void FinishIndex(long index)
        {
            lock (IndexLock)
            {
                if (WorkingIndexes.Contains(index))
                {
                    WorkingIndexes.Remove(index);
                    FinishedIndexes.Add(index);
                }
                while (FinishedIndexes.Contains(LastFinishedIndex))
                {
                    FinishedIndexes.Remove(LastFinishedIndex);
                    LastFinishedIndex++;
                }
            }
        }

        /// <summary>
        /// 任务异常, 将对应 Index 移除, 允许其它任务重新获取
        /// </summary>
        /// <param name="index"></param>
        public void ReleaseIndex(long index)
        {
            lock (IndexLock)
            {
                if (WorkingIndexes.Contains(index))
                {
                    WorkingIndexes.Remove(index);
                }
            }
        }
    }
}
