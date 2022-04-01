using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.Models.TransferLib
{
    /// <summary>
    /// 原计划作为 TransferFileInfo 和 TransferDirectoryInfo 的父类
    /// 现在没用上
    /// </summary>
    public class TransferInfo
    {
        #region Properties
        /// <summary>
        /// 父节点指针, TransferRootInfo 继承自 TransferDirectoryInfo
        /// 区别在于, Root的父节点为空, Directory的父节点不为空
        /// 不得在Root节点中调用 .Root 属性
        /// </summary>
        public TransferInfoDirectory Parent { get; set; } = null;

        /// <summary>
        /// 文件来源路径
        /// 对下载任务, 为Server端路径
        /// 对上传任务, 为本地路径
        /// </summary>
        public string Name { get; set; } = "";

        public long Length { get; set; } = 0;

        #endregion



        private TransferInfoRoot Root
        {
            get
            {
                System.Diagnostics.Debug.Assert(this.Parent != null);
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






    }
}
