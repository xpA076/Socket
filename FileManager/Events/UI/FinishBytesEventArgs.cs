using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.Events.UI
{
    public delegate void FinishBytesEventHandler(object sender, FinishBytesEventArgs e);

    public class FinishBytesEventArgs : EventArgs
    {
        public readonly long BytesCount;

        public FinishBytesEventArgs(long bytes_count)
        {
            BytesCount = bytes_count;
        }
    }
}
