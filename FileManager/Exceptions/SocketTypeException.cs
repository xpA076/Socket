using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.Exceptions
{
    /// <summary>
    /// Request 或 Response 类的Type不合法对应的异常
    /// 一般是因为序列化问题或非法访问 Respnose 属性
    /// </summary>
    public class SocketTypeException : Exception
    {
        public int NeedType { get; set; } = -1;
        public int ExceptionType { get; set; }

        public SocketTypeException()
        {

        }

        public SocketTypeException(object need_type, object exception_type)
        {
            NeedType = (int)need_type;
            ExceptionType = (int)exception_type;
        }

    }
}
