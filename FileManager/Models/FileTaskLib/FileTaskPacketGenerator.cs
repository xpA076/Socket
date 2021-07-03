using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.Models
{
    /// <summary>
    /// 在大文件传输中为子线程分配文件传输任务
    /// </summary>
    public class FileTaskPacketGenerator
    {
        private readonly object PacketLock = new object();
        private readonly HashSet<int> TransferingPackets = new HashSet<int>();
        private readonly HashSet<int> FinishedPackets = new HashSet<int>();

        public int FinishedPacket { get; private set; }

        public FileTask Task { get; set; }

        public FileTaskPacketGenerator(FileTask task)
        {

        }

        public void Reset(FileTask task)
        {

        }

        public int GeneratePacket()
        {

        }

        public void FinishPacket(int packet)
        {

        }

        public void ReleasePacket(int packet)
        {

        }
    }
}
