﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

using SocketLib;
using FileManager.Static;

namespace FileManager.Models
{
    /// <summary>
    /// 记录文件任务相关参数
    /// 包括 FileTask 内容, 传输字节计数 等
    /// 可保存为 xml 或从 xml 中恢复
    /// </summary>
    public class FileTaskRecord
    {
        #region 属性与变量
        private string RecordPath
        {
            get
            {
                return Path.GetDirectoryName(
                    System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName) +
                    "\\FileTransferRecord.xml";
            }
        }


        // 这里要用 ObservableCollection 不 能用 List
        // 实现引用不变内容改变下的实时显示
        public ObservableCollection<FileTask> FileTasks = new ObservableCollection<FileTask>();
        public int CurrentTaskIndex = 0; // CurrentTaskIndex 一直指向当前未完成的第一个任务

        private long PrevBytesAddup { get; set; } = 0; // 当前任务以前任务总byte数
        public long TotalLength { get; set; } = 0;
        public long TotalFinished 
        {
            get 
            {
                return PrevBytesAddup + (_need_clear ? 0 : CurrentFinished);
            } 
        }
        public long CurrentLength { get; set; } = 0;
        public long CurrentFinished { get; set; } = 0;

        private bool _need_clear = false;

        private DateTime LastSaveTime;

        #endregion

        public FileTaskRecord()
        {
            LastSaveTime = DateTime.Now;
        }

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

        public bool NeedSaveRecord()
        {
            return (DateTime.Now - LastSaveTime).TotalMilliseconds > Config.SaveRecordInterval;
        }

        /// <summary>
        /// 启动新任务时调用
        /// </summary>
        public void StartNewTask()
        {
            
            CurrentLength = CurrentTask.Length; // 更新 CurrentLength
            CurrentFinished = CurrentTask.FinishedPacket * HB32Encoding.DataSize;
            Logger.Log(string.Format("<FileTaskRecord> call StartNewTask, TotalLength={0}, TotalFinished={1}, CurrentLength={2}, CurrentFinished={3}, PrevBytesAddup={4}",
                TotalLength, TotalFinished, CurrentLength, CurrentFinished, PrevBytesAddup), LogLevel.Debug);
        }
        

        /// <summary>
        /// 完成任务时调用
        /// </summary>
        public void FinishCurrentTask()
        {
            PrevBytesAddup += CurrentLength; // 将当前完成任务 byte 数加入 taskAddup
            Logger.Log(string.Format("<FileTaskRecord> call FinishCurrentTask, TotalLength={0}, TotalFinished={1}, CurrentLength={2}, CurrentFinished={3}, PrevBytesAddup={4}",
                TotalLength, TotalFinished, CurrentLength, CurrentFinished, PrevBytesAddup), LogLevel.Debug);

        }


        public void AddTask(FileTask task)
        {
            if (_need_clear)
            {
                FileTasks.Clear();
                CurrentTaskIndex = 0;
                PrevBytesAddup = 0;
                TotalLength = 0;
                CurrentLength = 0;
                CurrentFinished = 0;
                _need_clear = false;
            }
            FileTasks.Add(task);
        }

        public void Clear()
        {
            _need_clear = true;
        }


        #region xml 持久化操作

        /// <summary>
        /// 保存当前任务
        /// </summary>
        public void SaveXml()
        {
            XElement root = new XElement("record");
            /// Length
            /*
            XElement len = new XElement("length");
            len.SetElementValue("currentLength", CurrentLength);
            len.SetElementValue("currentFinished", CurrentFinished);
            len.SetElementValue("totalLength", TotalLength);
            len.SetElementValue("taskAddup", PrevBytesAddup);
            root.Add(len);
            */
            /// Index
            XElement idx = new XElement("index");
            idx.SetElementValue("current", CurrentTaskIndex);
            root.Add(idx);
            /// Tasks
            XElement tasks = new XElement("taskCollection");
            foreach (FileTask fileTask in FileTasks)
            {
                XElement task = new XElement("task");
                task.SetElementValue("TcpAddress", fileTask.TcpAddress.ToString());
                task.SetElementValue("IsDirectory", fileTask.IsDirectory);
                task.SetElementValue("Type", fileTask.Type);
                task.SetElementValue("RemotePath", fileTask.RemotePath);
                task.SetElementValue("LocalPath", fileTask.LocalPath);
                task.SetElementValue("Length", fileTask.Length);
                task.SetElementValue("Status", fileTask.Status);
                task.SetElementValue("FinishedPacket", fileTask.FinishedPacket);
                tasks.Add(task);
            }
            /// 添加任务
            root.Add(tasks);
            root.Save(RecordPath);
            LastSaveTime = DateTime.Now;
            Logger.Log("Saved record.", LogLevel.Info);
        }

        /// <summary>
        /// 加载当前任务
        /// </summary>
        public void LoadXml()
        {
            if (!File.Exists(RecordPath))
            {
                System.Windows.Forms.MessageBox.Show(RecordPath + "do not exist");
                return;
            }
            XDocument doc = XDocument.Load(RecordPath);
            XElement root = doc.Root;
            /// Index
            XElement idx = root.Element("index");
            CurrentTaskIndex = int.Parse(idx.Element("current").Value);
            /// Tasks
            XElement tasks = root.Element("taskCollection");
            foreach (XElement task in tasks.Elements("task"))
            {
                FileTask fileTask = new FileTask();
                fileTask.TcpAddress = TCPAddress.FromString(task.Element("TcpAddress").Value);
                fileTask.IsDirectory = bool.Parse(task.Element("IsDirectory").Value);
                fileTask.Type = (FileTaskType)Enum.Parse(typeof(FileTaskType), task.Element("Type").Value);
                fileTask.RemotePath = task.Element("RemotePath").Value;
                fileTask.LocalPath = task.Element("LocalPath").Value;
                fileTask.Length = long.Parse(task.Element("Length").Value);
                fileTask.Status = (FileTaskStatus)Enum.Parse(typeof(FileTaskStatus), task.Element("Status").Value);
                fileTask.FinishedPacket = int.Parse(task.Element("FinishedPacket").Value);
                FileTasks.Add(fileTask);
            }
            /// Length
            /*
            XElement len = root.Element("length");
            CurrentLength = long.Parse(len.Element("currentLength").Value);
            CurrentFinished = long.Parse(len.Element("currentFinished").Value);
            TotalLength = long.Parse(len.Element("totalLength").Value);
            PrevBytesAddup = long.Parse(len.Element("taskAddup").Value);
            */
            CurrentLength = FileTasks[CurrentTaskIndex].Length;
            CurrentFinished = FileTasks[CurrentTaskIndex].FinishedPacket * HB32Encoding.DataSize;
            TotalLength = 0;
            PrevBytesAddup = 0;
            for (int i = 0; i < FileTasks.Count; ++i)
            {
                TotalLength += FileTasks[i].Length;
                if (i < CurrentTaskIndex)
                {
                    PrevBytesAddup += FileTasks[i].Length;
                }
            }
            Logger.Log("Loaded record.", LogLevel.Info);
        }
        #endregion
    }
}
