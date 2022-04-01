using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.Models.TransferLib
{
    public enum TransferStatus : int
    {
        Waiting,
        Success,
        //Denied,
        Failed,
        Transfering,

    }

}
