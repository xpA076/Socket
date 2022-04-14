using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.SocketLib.SocketServer
{

    /*
    public class SocketIdentity
    {
        private enum IdentityFlag : int
        {
            None = 0x0,
            ReadFile = 0x1,
            WriteFile = 0x2,
            RemoteRun = 0x8,
            All = 0xF
        }

        private IdentityFlag Flag { get; set; }

        public SocketIdentity(int flag)
        {
            Flag = (IdentityFlag)flag;
        }

        public static SocketIdentity All
        {
            get
            {
                return new SocketIdentity((int)IdentityFlag.All);
            }
        }

        public bool AllowReadFile()
        {
            return (Flag & IdentityFlag.ReadFile) > 0;
        }

        public bool AllowWriteFile()
        {
            return (Flag & IdentityFlag.WriteFile) > 0;
        }

        public bool AllowRemoteRun()
        {
            return (Flag & IdentityFlag.RemoteRun) > 0;
        }


    }

    */
}
