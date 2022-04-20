using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FileManager.Models.TransferLib.Services
{
    public class TransferThreadPool
    {
        public int ThreadLimit { get; set; } = 16;

        private readonly List<Thread> Threads = new List<Thread>();

        private readonly List<TransferThreadInfo> ThreadInfos = new List<TransferThreadInfo>();


        public void Initialize()
        {

        }
    }
}
