using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.Models
{
    public class TransferInfo
    {
        public TransferDirectoryInfo Parent = null;

        /// <summary>
        /// 文件来源路径
        /// 对下载任务, 为Server端路径
        /// 对上传任务, 为本地路径
        /// </summary>
        public string Name { get; set; }

        public string RelativePath 
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                TransferDirectoryInfo pt = this.Parent;
                while (pt != null)
                {
                    sb.Insert(0, "/" + pt.Name);
                    pt = pt.Parent;
                }
                sb.Append("/" + this.Name);
                return sb.ToString();
            }
        }


    }
}
