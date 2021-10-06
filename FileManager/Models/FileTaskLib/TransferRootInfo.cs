using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FileManager.SocketLib;
using FileManager.SocketLib.Enums;

namespace FileManager.Models
{
    public class TransferRootInfo : TransferDirectoryInfo
    {
        public TransferRootInfo()
        {
            this.Parent = null;
            this.Name = null;
        }


        public ConnectionRoute Route { get; set; }

        public TransferType Type { get; set; }

        public FilterRule Rule { get; set; }

        public string RemoteDirectory { get; set; }

        public string LocalDirectory { get; set; }



    }
}
