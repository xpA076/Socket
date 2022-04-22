using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FileManager.SocketLib;
using FileManager.Models.Serializable;
using FileManager.Models.TransferLib.Enums;

namespace FileManager.Models.TransferLib.Info
{

    /// <summary>
    /// 传输任务的文件夹信息, 同时作为 TransferInfoRoot的父类
    /// </summary>
    public class TransferInfoDirectory
    {
        #region Parameters to be saved
        public string Name { get; set; } = "";

        public long Length { get; set; } = 0;

        /// <summary>
        /// 初始为 false, 在构造过当前节点列表(列表中子节点未构建)后为 true
        /// 在 BuildChildrenFrom() 调用后为 true
        /// </summary>
        public bool IsChildrenListBuilt { get; set; } = false;

        public TransferStatus Status { get; set; } = TransferStatus.Querying;

        /// <summary>
        /// 在 BuildChildrenFrom() 中初始化, 长度和 DirectoryChildren 相同
        /// 初始全为 false, 目录及对应所有子目录全部构建完成后在 Querier 中置为 true
        /// </summary>
        public List<bool> QueryCompleteFlags { get; set; } = new List<bool>();


        /// <summary>
        /// 在 BuildChildrenFrom() 中初始化, 长度和 DirectoryChildren 相同
        /// </summary>
        public List<bool> TransferCompleteDirectoryFlags { get; set; } = new List<bool>();

        /// <summary>
        /// 在 BuildChildrenFrom() 中初始化, 长度和 FileChildren 相同
        /// </summary>
        public List<bool> TransferCompleteFileFlags { get; set; } = new List<bool>();


        public List<TransferInfoDirectory> DirectoryChildren { get; set; } = new List<TransferInfoDirectory>();

        public List<TransferInfoFile> FileChildren { get; set; } = new List<TransferInfoFile>();
        #endregion

        #region Parameters

        private TransferInfoRoot Root
        {
            get
            {
                if (this.Parent == null)
                {
                    return this as TransferInfoRoot;
                }
                TransferInfoDirectory pt = this.Parent;
                while (!pt.IsRoot)
                {
                    pt = pt.Parent;
                }
                return pt as TransferInfoRoot;
            }
        }


        /// <summary>
        /// 最终形如 "xxx" 或 "xxx/xxx/xxx"
        /// </summary>
        public string RelativePath
        {
            get
            {
                string path = this.Name;
                TransferInfoDirectory pt = this.Parent;
                while (!pt.IsRoot)
                {
                    path = pt.Name + "\\" + path;
                    pt = pt.Parent;
                }
                return path;
            }
        }

        public string RemotePath
        {
            get
            {
                return Path.Combine(Root.RemoteDirectory, RelativePath);
            }
        }

        public string LocalPath
        {
            get
            {
                return Path.Combine(Root.LocalDirectory, RelativePath);
            }
        }

        /// <summary>
        /// 父节点指针, TransferRootInfo 继承自 TransferDirectoryInfo
        /// 区别在于, Root的父节点为空, Directory的父节点不为空
        /// 不得在Root节点中调用 .Root 属性
        /// </summary>
        public TransferInfoDirectory Parent { get; set; } = null;

        public bool IsRoot
        {
            get
            {
                return this.Parent == null;
            }
        }

        public bool IsQueryComplete
        {
            get
            {
                if (IsChildrenListBuilt)
                {
                    /// 如果是 DFS 按顺序遍历, 这里可以简化成下面这种形式
                    return (DirectoryChildren.Count == 0) || QueryCompleteFlags.Last();
                }
                else
                {
                    return false;
                }
            }
        }
        #endregion


        /// <summary>
        /// 当前节点构造完成后调用 (当前节点为叶子节点或所有子节点已经过DFS构造完成后)
        /// </summary>
        public void CalculateLength()
        {
            this.Length = 0;
            foreach (TransferInfoDirectory info in DirectoryChildren)
            {
                Length += info.Length;
            }
            foreach (TransferInfoFile info in FileChildren)
            {
                Length += info.Length;
            }
        }



        public void BuildChildrenFrom(List<SocketFileInfo> socketFileInfos)
        {
            this.DirectoryChildren.Clear();
            this.FileChildren.Clear();
            foreach (var socketFileInfo in socketFileInfos)
            {
                if (socketFileInfo.IsDirectory)
                {
                    TransferInfoDirectory directoryInfo = new TransferInfoDirectory();
                    directoryInfo.Name = socketFileInfo.Name;
                    directoryInfo.Length = 0;
                    directoryInfo.Parent = this;
                    this.DirectoryChildren.Add(directoryInfo);
                    this.QueryCompleteFlags.Add(false);
                    this.TransferCompleteDirectoryFlags.Add(false);
                }
                else
                {
                    TransferInfoFile fileInfo = new TransferInfoFile();
                    fileInfo.Name = socketFileInfo.Name;
                    fileInfo.Length = socketFileInfo.Length;
                    fileInfo.CreationTimeUtc = socketFileInfo.CreationTimeUtc;
                    fileInfo.LastWriteTimeUtc = socketFileInfo.LastWriteTimeUtc;
                    fileInfo.Parent = this;
                    this.FileChildren.Add(fileInfo);
                    this.TransferCompleteFileFlags.Add(false);
                }
            }
            IsChildrenListBuilt = true;
        }


        /// <summary>
        /// 利用 FileStream 写入文件
        /// </summary>
        /// <param name="fs"></param>
        /// <returns>当前节点及所有子节点byte总长度(4 byte 长度头标识中为本节点长度)</returns>
        public int SaveToFile(FileStream fs)
        {
            /// 写入当前节点信息
            BytesBuilder bb = new BytesBuilder();
            bb.Append(Name);
            bb.Append(Length);
            bb.Append(IsChildrenListBuilt);
            bb.AppendListBool(QueryCompleteFlags);
            bb.AppendListBool(TransferCompleteDirectoryFlags);
            bb.AppendListBool(TransferCompleteFileFlags);
            byte[] bs = bb.GetBytes();
            fs.Write(BitConverter.GetBytes(bs.Length), 0, 4);
            fs.Write(bs, 0, bs.Length);
            /// 写入子节点信息
            int child_len = 0;
            fs.Write(BitConverter.GetBytes(DirectoryChildren.Count), 0, 4);
            foreach (var child in DirectoryChildren)
            {
                child_len += child.SaveToFile(fs);
            }
            fs.Write(BitConverter.GetBytes(FileChildren.Count), 0, 4);
            foreach (var child in FileChildren)
            {
                child_len += child.SaveToFile(fs);
            }
            //_bytes_length = 4 + bs.Length + 8 + child_len;
            //return _bytes_length;
            return 0;
        }


        public static TransferInfoDirectory ReadFromFile(FileStream fs)
        {
            /// 构建当前节点
            byte[] b_len = new byte[4];
            fs.Read(b_len, 0, 4);
            int len = BitConverter.ToInt32(b_len, 0);
            byte[] bs = new byte[len];
            fs.Read(bs, 0, len);
            TransferInfoDirectory info_dir = new TransferInfoDirectory();
            int idx = 0;
            info_dir.Name = BytesParser.GetString(bs, ref idx);
            info_dir.Length = BytesParser.GetLong(bs, ref idx);
            info_dir.IsChildrenListBuilt = BytesParser.GetBool(bs, ref idx);
            info_dir.QueryCompleteFlags = BytesParser.GetListBool(bs, ref idx);
            info_dir.TransferCompleteDirectoryFlags = BytesParser.GetListBool(bs, ref idx);
            info_dir.TransferCompleteFileFlags = BytesParser.GetListBool(bs, ref idx);
            /// 构建子节点
            fs.Read(b_len, 0, 4);
            len = BitConverter.ToInt32(b_len, 0);
            for (int i = 0; i < len; ++i)
            {
                TransferInfoDirectory info = TransferInfoDirectory.ReadFromFile(fs);
                info.Parent = info_dir;
                info_dir.DirectoryChildren.Add(info);
            }
            fs.Read(b_len, 0, 4);
            len = BitConverter.ToInt32(b_len, 0);
            for (int i = 0; i < len; ++i)
            {
                TransferInfoFile info = TransferInfoFile.ReadFromFile(fs);
                info.Parent = info_dir;
                info_dir.FileChildren.Add(info);
            }
            return info_dir;
        }



    }
}
