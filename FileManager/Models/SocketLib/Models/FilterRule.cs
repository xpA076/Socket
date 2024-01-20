using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.Models.SocketLib.Models
{
    /// <summary>
    /// 文件列表信息请求时的过滤规则
    /// </summary>
    public class FilterRule
    {
        public byte[] GetBytes()
        {
            return new byte[4];
        }

        public static FilterRule FromBytes(byte[] bytes)
        {
            return new FilterRule();
        }
    }
}
