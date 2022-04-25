using FileManager.Events.UI;
using FileManager.Models.TransferLib;
using FileManager.Models.TransferLib.Enums;
using FileManager.Models.TransferLib.Info;
using FileManager.Models.TransferLib.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FileManager.ViewModels.PageTransfer
{
    public class PageTransferViewModel : INotifyPropertyChanged
    {
        public const int RefreshInterval = 500;

        public event PropertyChangedEventHandler PropertyChanged;

        private string _transfer_status = "Transfer status -> ";

        public string TransferStatus
        {
            set
            {
                _transfer_status = "Transfer status -> " + value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("TransferStatus"));
            }
            get
            {
                return _transfer_status;
            }
        }

        /// <summary>
        /// 这个与 TransferManager 共用同一个InfoRoot列表
        /// </summary>
        public List<TransferInfoRoot> InfoRoots;

        private int CurrentInfoRootIndex = 0;

        public readonly ObservableCollection<ListViewTransferItem> ListViewItems = new ObservableCollection<ListViewTransferItem>();

        private readonly List<ListViewTransferItem> OpenedItems = new List<ListViewTransferItem>();

        public enum ProgressType : int
        {
            Bytes,
            Percent,
        }
        
        private string _current_progress;

        public ProgressType CurrentProgressType = ProgressType.Bytes;

        public string CurrentProgress
        {
            protected set
            {
                _current_progress = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CurrentProgress"));
            }
            get
            {
                return _current_progress;
            }
        }

        private string _total_progress;

        public ProgressType TotalProgressType = ProgressType.Bytes;

        public string TotalProgress
        {
            protected set
            {
                _total_progress = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("TotalProgress"));
            }
            get
            {
                return _total_progress;
            }
        }

        private string _speed;

        public string Speed
        {
            protected set
            {
                _speed = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Speed"));
            }
            get
            {
                return _speed;
            }
        }

        private string _time_remaining;

        public string TimeRemaining
        {
            protected set
            {
                _time_remaining = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("TimeRemaining"));
            }
            get
            {
                return _time_remaining;
            }
        }


        /// <summary>
        /// 此任务以前的传输完成任务累计字节数
        /// </summary>
        private long TotalFinished { get; set; }

        /// <summary>
        /// 此任务以前的传输失败任务累计字节数
        /// </summary>
        private long TotalFailed { get; set; }

        private long TotalLength { get; set; }

        private long CurrentFinished { get; set; }

        private long CurrentLength { get; set; }

        public long TicTokFinishedBytes = 0;

        public DateTime LastRefreshTime = DateTime.Now;

        private bool IsRefreshingUI = false;

        /// <summary>
        /// 指示当前传输过程在某一任务中, 还是处于任务外 (影响 Progress UI 的更新逻辑)
        /// </summary>
        private bool IsInProgress = false;

        /// <summary>
        /// 将此 Flag 置为 true 可以防止在下一个刷新周期内刷新 UI
        /// </summary>
        private bool IsUpdated = false;

        #region about ListView


        public void ListViewOpenOrCloseDirectory(ListViewTransferItem item)
        {
            /// OpenedItems 中对应item的索引
            int opened_idx = 0;
            bool found = false;
            for (opened_idx = 0; opened_idx < OpenedItems.Count; ++opened_idx)
            {
                int result = OpenedItems[opened_idx].CompareTo(item);
                if (result == 0)
                {
                    found = true;
                    break;
                }
                else if (result > 0)
                {
                    found = false;
                    break;
                }
            }
            /// 若 found == true, opened_idx 为与 item 相同的 OpenedItems 索引
            /// 若 found == false, opened_idx 为第一个比 item 大的 OpenedItems 索引
            ///   若均小于item, 则 opened_idx 为 OpenedItem.Count (即插入item的索引)
            if (found)
            {
                /// 关闭已展开文件夹
                OpenedItems.RemoveAt(opened_idx);
                RemoveListViewItemChildren(item);
            }
            else
            {
                /// 展开文件夹
                OpenedItems.Insert(opened_idx, item);
                InsertListViewItemChildren(item);
            }

        }


        private void InsertListViewItemChildren(ListViewTransferItem item)
        {
            /// idx 为当前选中 item 在 ListView 中的索引
            int idx;
            for (idx = 0; idx < ListViewItems.Count; ++idx)
            {
                if (ListViewItems[idx].Equals(item))
                {
                    break;
                }
            }
            TransferInfoDirectory directory = FindDirectoryInfo(item);
            int pt = idx + 1;
            for (int i = 0; i < directory.DirectoryChildren.Count; ++i)
            {
                TransferInfoDirectory info = directory.DirectoryChildren[i];
                ListViewTransferItem it = new ListViewTransferItem
                {
                    TaskIndex = item.TaskIndex,
                    Level = item.Level + 1,
                    IsDownload = item.IsDownload,
                    IsDirectory = true,
                    RelativePath = info.RelativePath,
                    Name = info.Name,
                    Size = info.Length,
                    Status = info.Status
                };
                ListViewItems.Insert(pt, it);
                ++pt;
            }
            for (int i = 0; i < directory.FileChildren.Count; ++i)
            {
                TransferInfoFile info = directory.FileChildren[i];
                ListViewTransferItem it = new ListViewTransferItem
                {
                    TaskIndex = item.TaskIndex,
                    Level = item.Level + 1,
                    IsDownload = item.IsDownload,
                    IsDirectory = false,
                    RelativePath = info.RelativePath,
                    Name = info.Name,
                    Size = info.Length,
                    Status = info.Status
                };
                ListViewItems.Insert(pt, it);
                ++pt;
            }
        }


        private void RemoveListViewItemChildren(ListViewTransferItem item)
        {
            int idx;
            for (idx = 0; idx < ListViewItems.Count; ++idx)
            {
                if (ListViewItems[idx].Equals(item))
                {
                    break;
                }
            }
            TransferInfoDirectory directory = FindDirectoryInfo(item);
            int begin = idx + 1;
            int end;
            for (end = begin; end < ListViewItems.Count; ++end)
            {
                if (ListViewItems[end].Level > item.Level)
                {
                    continue;
                }
                else
                {
                    break;
                }
            }
            for (int i = Math.Min(end, ListViewItems.Count - 1); i >= begin; --i)
            {
                ListViewItems.RemoveAt(i);
            }
        }


        private TransferInfoDirectory FindDirectoryInfo(ListViewTransferItem item)
        {
            TransferInfoRoot root = InfoRoots[item.TaskIndex];
            if (item.Level == 0) { return root; }
            string[] splits = item.RelativePath.Split('\\');
            TransferInfoDirectory pt = root;
            for (int si = 0; si < splits.Length; ++si)
            {
                string name = splits[si];
                foreach(TransferInfoDirectory td in pt.DirectoryChildren)
                {
                    if (name == td.Name)
                    {
                        pt = td;
                        break;
                    }
                }
            }
            return pt;
        }


        public void UpdateFileStatus(TransferInfoFile info, TransferStatus new_status)
        {
            UpdateStatus(info.RelativePath, false, info.Root, new_status);
        }


        public void UpdateDirectoryStatus(TransferInfoDirectory info, TransferStatus new_status)
        {
            UpdateStatus(info.RelativePath, true, info.Root, new_status);
        }


        /// <summary>
        /// 更新当前 ListViewItem 的 TransferStatus
        /// </summary>
        private void UpdateStatus(string relative_path, bool is_directory, TransferInfoRoot root, TransferStatus new_status)
        {
            foreach (ListViewTransferItem item in this.ListViewItems)
            {
                if (item.IsDirectory == is_directory && item.RelativePath == relative_path)
                {
                    if (InfoRoots[item.TaskIndex] == root)
                    {
                        item.Status = new_status;
                        return;
                    }
                }
            }
        }


        public void UpdateRootSize(TransferInfoRoot root, long size)
        {
            foreach (ListViewTransferItem item in this.ListViewItems)
            {
                if (item.Level == 0 && InfoRoots[item.TaskIndex] == root)
                {
                    item.Size = size;
                    return;
                }
            }
        }



        public void AfterAddNewRoot()
        {
            // todo 添加新节点对应的OpenedItems
            ListViewItems.Add(new ListViewTransferItem
            {
                TaskIndex = InfoRoots.Count - 1,
                Level = 0,
                IsDownload = InfoRoots.Last().Type == TransferType.Download,
                IsDirectory = true,
                RelativePath = "",
                Name = "Task " + InfoRoots.Count,
            });
        }

        #endregion




        /// <summary>
        /// 设定当前传输任务为 InfoRoots 的 index 位置, 应按这个更新 Progress
        /// </summary>
        /// <param name="index"></param>
        public void SetCurrentRoot(int index)
        {
            CurrentInfoRootIndex = index;
            TotalLength = InfoRoots[index].Length;
        }


        /// <summary>
        /// 启动 RefreshCycle(), 周期性更新进度 和 speed 等信息
        /// </summary>
        public void StartRefresh()
        {
            Task.Run(() => { RefreshCycle(); });
        }


        private void RefreshCycle()
        {
            IsRefreshingUI = true;
            LastRefreshTime = DateTime.Now;
            while (IsRefreshingUI)
            {
                Thread.Sleep(RefreshInterval);
                if (IsUpdated)
                {
                    IsUpdated = false;
                    continue;
                }
                UpdateSpeed();
                UpdateProgress();
            }
        }


        public void StopRefresh()
        {
            IsRefreshingUI = false;
        }


        public void OnFinishBytes(object sender, FinishBytesEventArgs e)
        {
            TicTokFinishedBytes += e.BytesCount;
        }

        /// <summary>
        /// 响应 Transfer 新任务时的调用, 更新对应 CurrentLength 等信息
        /// </summary>
        /// <param name="file"></param>
        public void SetNewFile(TransferInfoFile file)
        {
            ResetCounter();
            /// 切换至新任务
            CurrentFinished = file.FinishedPacket * TransferDiskManager.BlockSize;
            CurrentLength = file.Length;
            IsUpdated = true;
            IsInProgress = true;
            UpdateProgress();
        }


        /// <summary>
        /// 当前任务传输完成, 更新UI记录
        /// </summary>
        public void CurrentFileFinished()
        {
            CurrentFinished = CurrentLength;
            TotalFinished += CurrentLength;
            IsUpdated = true;
            IsInProgress = false;
            UpdateProgress();
        }


        /// <summary>
        /// 当前任务传输失败, 更新UI记录
        /// </summary>
        public void CurrentFileFailed()
        {
            CurrentFinished = 0;
            TotalFailed += CurrentLength;
            IsUpdated = true;
            IsInProgress = false;
            UpdateProgress();
        }


        public void ChangeCurrentProgressDisplay()
        {
            if (CurrentProgressType == ProgressType.Bytes)
            {
                CurrentProgressType = ProgressType.Percent;
            }
            else
            {
                CurrentProgressType = ProgressType.Bytes;
            }
            UpdateProgress();
        }


        public void ChangeTotalProgressDisplay()
        {
            if (TotalProgressType == ProgressType.Bytes)
            {
                TotalProgressType = ProgressType.Percent;
            }
            else
            {
                TotalProgressType = ProgressType.Bytes;
            }
            UpdateProgress();
        }


        /// <summary>
        /// 刷新 Speed 和 TimeRemaining, 而后重置计数器
        /// </summary>
        private void UpdateSpeed()
        {
            DateTime tok = DateTime.Now;
            int ms = (tok - LastRefreshTime).Milliseconds;
            double speed = (double)TicTokFinishedBytes / ((double)ms / 1000);
            /// Speed
            Speed = string.Format("{0, 18}/s", SizeToString(speed));

            /// TimeRemaining
            if (speed == 0)
            {
                if (TotalFinished + TotalFailed == TotalLength)
                {
                    TimeRemaining = string.Format("{0, 16}", SecondsToString(0));
                }
                else
                {
                    TimeRemaining = string.Format("{0, 16}", "--");
                }
            }
            else
            {
                int seconds = (int)((TotalLength - TotalFinished - TotalFailed - CurrentFinished) / speed);
                TimeRemaining = string.Format("{0, 16}", SecondsToString(seconds));
            }
            /// 重置计数器
            CurrentFinished += TicTokFinishedBytes;
            ResetCounter();
        }


        /// <summary>
        /// 刷新传输进度
        /// </summary>
        private void UpdateProgress()
        {
            long top = 0;
            long bot = 0;
            /// CurrentProgress
            top = CurrentFinished;
            bot = CurrentLength;
            if (CurrentProgressType == ProgressType.Bytes)
            {
                CurrentProgress = string.Format("{0, 12}/{1}", SizeToString(top), SizeToString(bot));
            }
            else
            {
                double percent = 0;
                if (bot > 0)
                {
                    percent = (double)top * 100 / bot;
                }
                CurrentProgress = string.Format("{0, 16:0.00} %", percent);
            }

            /// TotalProgress
            if (IsInProgress)
            {
                top = TotalFinished + TotalFailed + CurrentFinished;
            }
            else
            {
                top = TotalFinished + TotalFailed;
            }
            bot = TotalLength;
            if (TotalProgressType == ProgressType.Bytes)
            {
                TotalProgress = string.Format("{0, 12}/{1}", SizeToString(top), SizeToString(bot));
            }
            else
            {
                double percent = 0;
                if (bot > 0)
                {
                    percent = (double)top * 100 / bot;
                }
                TotalProgress = string.Format("{0, 16:0.00} %", percent);
            }
        }


        private void ResetCounter()
        {
            TicTokFinishedBytes = 0;
            LastRefreshTime = DateTime.Now;
        }


        private string SecondsToString(int seconds)
        {
            return string.Format("{0}:{1:D2}:{2:D2}", seconds / 3600, (seconds % 3600) / 60, seconds % 60);
        }

        public static string SizeToString(double num)
        {
            if (num > 1 << 30)
            {
                double d = num / (1 << 30);
                return d.ToString("0.00") + " G";
            }
            else if (num > 1 << 20)
            {
                double d = num / (1 << 20);
                return d.ToString("0.00") + " M";
            }
            else if (num > 1 << 10)
            {
                double d = num / (1 << 10);
                return d.ToString("0.00") + " K";
            }
            else
            {
                return num.ToString("0.00") + " B";
            }
        }

    }
}
