using FileManager.SocketLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.Models
{
    public class TransferDirectoryInfo : TransferInfo
    {
        #region Parameters to be saved
        /// <summary>
        /// 初始为 false, 在构造过当前节点列表(列表中子节点未构建)后为 true
        /// </summary>
        public bool IsChildrenListBuilt { get; set; } = false;


        public List<bool> QueryCompleteFlags { get; set; } = new List<bool>();


        /// <summary>
        /// 在 BuildChildrenFrom() 中初始化, 长度和 DirectoryChildren 相同
        /// </summary>
        public List<bool> TransferCompleteFlags { get; set; } = new List<bool>();

        public List<TransferDirectoryInfo> DirectoryChildren { get; set; } = new List<TransferDirectoryInfo>();

        public List<TransferFileInfo> FileChildren { get; set; } = new List<TransferFileInfo>();
        #endregion

        #region Parameters

        /// <summary>
        /// 这个或许可用于记录存档 bias, 快速更新
        /// </summary>
        private int _bytes_length = 0;
        public virtual int BytesLength
        {
            get
            {
                if (_bytes_length == 0)
                {
                    int child_len = 0;
                    foreach (var child in DirectoryChildren)
                    {
                        child_len += child.BytesLength;
                    }
                    foreach (var child in FileChildren)
                    {
                        child_len += child.BytesLength;
                    }
                    _bytes_length = 4 + Encoding.UTF8.GetBytes(Name).Length + 4 + 4 + child_len;
                }
                return _bytes_length;
            }
        }


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
                    /*
                    foreach (bool flag in QueryCompleteFlags)
                    {
                        if (!flag)
                        {
                            return false;
                        }
                    }
                    return true;
                    */

                    /// 如果是 DFS 按顺序遍历, 这里可以简化成下面这种形式
                    return (DirectoryChildren.Count == 0) || QueryCompleteFlags.Last();
                    /*
                    if (DirectoryChildren.Count == 0) 
                    { 
                        return true; 
                    }
                    else 
                    { 
                        return QueryCompleteFlags.Last();
                    }
                    */
                }
                else
                {
                    return false;
                }
                //return IsChildrenListBuilt && (QueryCompleteCount == DirectoryChildren.Count);
            }
        }


        /*
        /// <summary>
        /// 根据 QueryCompleteFlags 查询第一个未 query 的子节点
        /// 在 Querier 中调用
        /// </summary>
        public TransferDirectoryInfo FirstUnqueriedChild
        {
            get
            {
                System.Diagnostics.Debug.Assert(this.IsChildrenListBuilt);
                for (int i = 0; i < DirectoryChildren.Count; i++)
                {
                    if (!QueryCompleteFlags[i])
                    {
                        return DirectoryChildren[i];
                    }
                }
                throw new Exception("TransferDirectoryInfo : Cannot find FirstUnqueriedChild.");
            }
        }

        */
        #endregion






        /// <summary>
        /// 当前节点构造完成后调用 (当前节点为叶子节点或所有子节点已经过DFS构造完成后)
        /// </summary>
        public void CalculateLength()
        {
            this.Length = 0;
            foreach (TransferDirectoryInfo info in DirectoryChildren)
            {
                Length += info.Length;
            }
            foreach (TransferFileInfo info in FileChildren)
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
                    TransferDirectoryInfo directoryInfo = new TransferDirectoryInfo();
                    directoryInfo.Name = socketFileInfo.Name;
                    directoryInfo.Length = 0;
                    //directoryInfo.QueryCompleteCount = 0;
                    //directoryInfo.TransferCompleteCount = 0;
                    directoryInfo.Parent = this;
                    this.DirectoryChildren.Add(directoryInfo);
                    this.QueryCompleteFlags.Add(false);
                }
                else
                {
                    TransferFileInfo fileInfo = new TransferFileInfo();
                    fileInfo.Name = socketFileInfo.Name;
                    fileInfo.Length = socketFileInfo.Length;
                    fileInfo.CreationTimeUtc = socketFileInfo.CreationTimeUtc;
                    fileInfo.LastWriteTimeUtc = socketFileInfo.LastWriteTimeUtc;
                    fileInfo.Parent = this;
                    this.FileChildren.Add(fileInfo);
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
            bb.Append(QueryCompleteFlags);
            bb.Append(TransferCompleteFlags);
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
            _bytes_length = 4 + bs.Length + 8 + child_len;
            return _bytes_length;
        }


        public static TransferDirectoryInfo ReadFromFile(FileStream fs)
        {
            /// 构建当前节点
            byte[] b_len = new byte[4];
            fs.Read(b_len, 0, 4);
            int len = BitConverter.ToInt32(b_len, 0);
            byte[] bs = new byte[len];
            fs.Read(bs, 0, len);
            TransferDirectoryInfo info_dir = new TransferDirectoryInfo();
            int idx = 0;
            info_dir.Name = BytesParser.GetString(bs, ref idx);
            info_dir.Length = BytesParser.GetLong(bs, ref idx);
            info_dir.IsChildrenListBuilt = BytesParser.GetBool(bs, ref idx);
            info_dir.QueryCompleteFlags = BytesParser.GetListBool(bs, ref idx);
            info_dir.TransferCompleteFlags = BytesParser.GetListBool(bs, ref idx);
            /// 构建子节点
            fs.Read(b_len, 0, 4);
            len = BitConverter.ToInt32(b_len, 0);
            for (int i = 0; i < len; ++i)
            {
                TransferDirectoryInfo info = TransferDirectoryInfo.ReadFromFile(fs);
                info.Parent = info_dir;
                info_dir.DirectoryChildren.Add(info);
            }
            fs.Read(b_len, 0, 4);
            len = BitConverter.ToInt32(b_len, 0);
            for (int i = 0; i < len; ++i)
            {
                TransferFileInfo info = TransferFileInfo.ReadFromFile(fs);
                info.Parent = info_dir;
                info_dir.FileChildren.Add(info);
            }
            return info_dir;
        }
    }
}
