using FileManager.SocketLib;
using FileManager.SocketLib.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.Exceptions
{
    public class SocketFlagException : Exception
    {
        public SocketPacketFlag RequiredFlag { get; set; } = SocketPacketFlag.None;
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

        public SocketFlagException(SocketPacketFlag required_flag, HB32Header header, byte[] bytes)
        {
            RequiredFlag = required_flag;
            Header = header;
            Bytes = bytes;
        }

    }
}
