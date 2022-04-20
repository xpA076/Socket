using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FileManager.Models.TransferLib.Services
{
    public class TransferThreadInfo
    {
        public AutoResetEvent Signal = new AutoResetEvent(false);

        public bool IsExit { get; set; } = false;
    }
}
