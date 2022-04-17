using FileManager.SocketLib;
using FileManager.SocketLib.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.Exceptions
{
    /// <summary>
    /// Receive 的 SocketPacketFlag 与预期不符
    /// 正想办法可否用 Response 类中的内容代替 22.04.14
    /// </summary>
    public class SocketFlagException : Exception
    {
        public HB32Packet RequiredFlag { get; set; } = HB32Packet.None;
        public HB32Header Header { get; set; } = null;
        public byte[] Bytes { get; set; } = new byte[0];

        public override string Message
        {
            get
            {
                string err_msg = "";
                try
                {
                    err_msg = Encoding.UTF8.GetString(Bytes);
                }
                catch (Exception) {; }
                return string.Format("[Received not valid header: {0}, required : {1} -- {2}]", 
                    Header.Flag.ToString(), RequiredFlag.ToString(), err_msg);
            }
        }

        public SocketFlagException()
        {

        }


        public SocketFlagException(HB32Header header, byte[] bytes)
        {
            Header = header;
            Bytes = bytes;
        }

        public SocketFlagException(HB32Packet required_flag, HB32Response resp)
        {
            RequiredFlag = required_flag;
            Header = resp.Header;
            Bytes = resp.Bytes;
        }


        public SocketFlagException(HB32Packet required_flag, HB32Header header, byte[] bytes)
        {
            RequiredFlag = required_flag;
            Header = header;
            Bytes = bytes;
        }

    }
}
