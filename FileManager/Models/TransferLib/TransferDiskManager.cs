using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.Models.TransferLib
{
    public partial class TransferDispatcher
    {
        private class BufferBlock
        {
            public int Index;
            public byte[] Bytes;
        }


        private class BufferQueue
        {
            private const int Capacity = 128;

            private BufferBlock[] blocks;

            public BufferQueue()
            {
                blocks = new BufferBlock[Capacity];
            }

            public void Enqueue()
            {

            }


        }


        private class TransferDiskManager
        {
            public void FinishDownloadPacket(TransferTaskBlock block, byte[] bytes)
            {

            }

        }


    }

}
