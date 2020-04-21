using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

using SocketFileManager.SocketLib;

namespace SocketFileManager.Models
{
    public class TransferRecord
    {
        #region 属性与变量
        private string recordPath
        {
            get
            {
                return Path.GetDirectoryName(
                    System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName) + 
                    "\\FileTransferRecord.xml";
            }
        }

        public long updateLengthThres = 128 * 1024; // 刷新界面最小字节数
        public int updateTimeThres = 500; // 刷新界面最短时间间隔

        // 这里要用 ObservableCollection 不 能用 List
        // 实现引用不变内容改变下的实时显示
        public ObservableCollection<FileTask> FileTasks = new ObservableCollection<FileTask>();
        public int CurrentTaskIndex = 0; // CurrentTaskIndex 一直指向当前未完成的第一个任务

        private long taskAddup = 0; // 当前任务以前任务总byte数
        public long TotalLength { get; set; } = 0;
        public long TotalFinished { get { return taskAddup + CurrentFinished; } }
        public long CurrentLength { get; set; } = 0;
        public long CurrentFinished
        {
            get { return (long)(CurrentTask.LastPackage + 1) * HB32Encoding.DataSize; }
        }


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
            CurrentLength = CurrentTask.Length; // 更新 CurrentLength
        }

        /// <summary>
        /// 完成任务时调用
        /// </summary>
        public void FinishCurrentTask()
        {
            taskAddup += CurrentLength; // 将当前完成任务 byte 数加入 taskAddup
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
            taskAddup = 0;
            TotalLength = 0;
            CurrentLength = 0;
            newBytes = 0;
        }

        /// <summary>
        /// 保存当前任务
        /// </summary>
        public void Save()
        {
            XDocument doc = new XDocument();
            XElement root = new XElement("record");
            XElement len = new XElement("length");
            len.SetElementValue("current", CurrentLength);
            len.SetElementValue("total", TotalLength);
            len.SetElementValue("taskAddup", taskAddup);
            root.Add(len);
            XElement idx = new XElement("index");
            idx.SetElementValue("current", CurrentTaskIndex);
            root.Add(idx);
            XElement tasks = new XElement("taskCollection");
            foreach (FileTask fileTask in FileTasks)
            {
                XElement task = new XElement("task");
                task.SetElementValue("isDir", fileTask.IsDirectory);
                task.SetElementValue("type", fileTask.Type);
                task.SetElementValue("remotePath", fileTask.RemotePath);
                task.SetElementValue("localPath", fileTask.LocalPath);
                task.SetElementValue("length", fileTask.Length);
                task.SetElementValue("status", fileTask.Status);
                task.SetElementValue("lastPackage", fileTask.LastPackage);
                tasks.Add(task);
            }
            // 添加任务
            root.Add(tasks);
            root.Save(recordPath);
        }

        /// <summary>
        /// 加载当前任务
        /// </summary>
        public void Load()
        {
            if (!File.Exists(recordPath))
            {
                System.Windows.Forms.MessageBox.Show(recordPath + "do not exist");
                return;
            }
            XDocument doc = XDocument.Load(recordPath);
            XElement root = doc.Root;
            XElement len = root.Element("length");
            CurrentLength = long.Parse(len.Element("current").Value);
            TotalLength = long.Parse(len.Element("total").Value);
            taskAddup = long.Parse(len.Element("taskAddup").Value);
            CurrentTaskIndex = int.Parse(root.Element("index").Element("current").Value);
            XElement tasks = root.Element("taskCollection");
            //FileTasks = new ObservableCollection<FileTask>();
            foreach (XElement task in tasks.Elements("task"))
            {
                FileTask fileTask = new FileTask();
                fileTask.IsDirectory = bool.Parse(task.Element("isDir").Value);
                fileTask.Type = task.Element("type").Value;
                fileTask.RemotePath = task.Element("remotePath").Value;
                fileTask.LocalPath = task.Element("localPath").Value;
                fileTask.Length = long.Parse(task.Element("length").Value);
                fileTask.Status = task.Element("status").Value;
                fileTask.LastPackage = int.Parse(task.Element("lastPackage").Value);
                FileTasks.Add(fileTask);
            }
        }
    }
}
