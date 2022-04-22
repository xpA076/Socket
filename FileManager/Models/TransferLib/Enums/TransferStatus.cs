using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.Models.TransferLib.Enums
{
    public enum TransferStatus : int
    {
        Querying,
        Waiting,
        Transfering,
        Pause,
        Finished,
        Failed,
    }

}
