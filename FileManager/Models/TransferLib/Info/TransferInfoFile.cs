using FileManager.Models.TransferLib.Enums;
using FileManager.Utils.Bytes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.Models.TransferLib.Info
{
    /// <summary>
    /// 传输任务的文件信息
    /// </summary>
    public class TransferInfoFile
    {

        public string Name { get; set; } = "";

        //public int Priority { get; set; } = 0;

        public long Length { get; set; } = 0;

        public DateTime CreationTimeUtc { get; set; } = new DateTime(0);

        public DateTime LastWriteTimeUtc { get; set; } = new DateTime(0);

        /// <summary>
        /// 当前完成的 packet 数量, 读写不加锁
        /// 已保证在完成对应packet后才会更新数据, 因此不存在未完成的packet被记录
        /// </summary>
        public long FinishedPacket { get; set; } = 0;

        public TransferStatus Status { get; set; } = TransferStatus.Waiting;


        /// <summary>
        /// 父节点指针, TransferRootInfo 继承自 TransferDirectoryInfo
        /// 区别在于, Root的父节点为空, Directory的父节点不为空
        /// 不得在Root节点中调用 .Root 属性
        /// </summary>
        public TransferInfoDirectory Parent { get; set; } = null;

        public TransferInfoRoot Root
        {
            get
            {
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
        /// 利用 FileStream 写入文件
        /// </summary>
        /// <param name="fs"></param>
        /// <returns>当前节点字节总长度(4 byte 长度头标识中为后续长度)</returns>
        public int SaveToFile(FileStream fs)
        {
            BytesBuilder bb = new BytesBuilder();
            bb.Append(Name);
            bb.Append(Length);
            bb.Append(CreationTimeUtc);
            bb.Append(LastWriteTimeUtc);
            bb.Append(FinishedPacket);
            bb.Append((int)Status);
            byte[] bs = bb.GetBytes();
            fs.Write(BitConverter.GetBytes(bs.Length), 0, 4);
            fs.Write(bs, 0, bs.Length);
            //_bytes_length = 4 + bs.Length;
            //return _bytes_length;
            return 0;
        }


        public static TransferInfoFile ReadFromFile(FileStream fs)
        {
            byte[] b_len = new byte[4];
            fs.Read(b_len, 0, 4);
            int len = BitConverter.ToInt32(b_len, 0);
            byte[] bs = new byte[len];
            fs.Read(bs, 0, len);
            TransferInfoFile info = new TransferInfoFile();
            int idx = 0;
            info.Name = BytesParser.GetString(bs, ref idx);
            info.Length = BytesParser.GetLong(bs, ref idx);
            info.CreationTimeUtc = BytesParser.GetDateTime(bs, ref idx);
            info.LastWriteTimeUtc = BytesParser.GetDateTime(bs, ref idx);
            info.FinishedPacket = BytesParser.GetLong(bs, ref idx);
            info.Status = (TransferStatus)BytesParser.GetInt(bs, ref idx);
            return info;
        }




    }
}
