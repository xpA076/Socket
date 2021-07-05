using FileManager.SocketLib;
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

        /// <summary>
        /// 已完成 packet 数量
        /// </summary>
        public int FinishedPacket { get; private set; }

        private int TotalPacket { get; set; }

        public FileTask Task { get; set; }

        public FileTaskPacketGenerator(FileTask task)
        {
            Task = task;
            FinishedPacket = task.FinishedPacket;
            TotalPacket = (int)(task.Length / HB32Encoding.DataSize) + (task.Length % HB32Encoding.DataSize > 0 ? 1 : 0);
        }

        public void Reset(FileTask task)
        {
            Task = task;
            FinishedPacket = task.FinishedPacket;
            TotalPacket = (int)(task.Length / HB32Encoding.DataSize) + (task.Length % HB32Encoding.DataSize > 0 ? 1 : 0);
            lock (this.PacketLock)
            {
                TransferingPackets.Clear();
                FinishedPackets.Clear();
            }
        }


        /// <summary>
        /// 申请获取任务packet index, 任务完成则返回 -1
        /// 根据 packet 数目更新 UI
        /// </summary>
        /// <returns> packet index </returns>
        public int GeneratePacket()
        {
            lock (this.PacketLock)
            {
                int packet = FinishedPacket;
                while (packet < TotalPacket)
                {
                    if (TransferingPackets.Contains(packet) || FinishedPackets.Contains(packet))
                    {
                        packet++;
                        continue;
                    }
                    else
                    {
                        TransferingPackets.Add(packet);
                        return packet;
                    }
                }
                return -1;
            }
        }


        /// <summary>
        /// packet 完成写入后清除记录并修正完成package数目
        /// </summary>
        /// <param name="packet">完成写入的packet index</param>
        public void FinishPacket(int packet)
        {
            lock (this.PacketLock)
            {
                if (TransferingPackets.Contains(packet))
                {
                    TransferingPackets.Remove(packet);
                    FinishedPackets.Add(packet);
                }
                while (FinishedPackets.Contains(FinishedPacket))
                {
                    FinishedPackets.Remove(FinishedPacket);
                    FinishedPacket++;
                }
            }
        }


        /// <summary>
        /// packet 传输过程异常, 解除当前 pakcet 占用
        /// (成功建立连接后再重新申请packet)
        /// </summary>
        /// <param name="packet"></param>
        public void ReleasePacket(int packet)
        {
            lock (this.PacketLock)
            {
                TransferingPackets.Remove(packet);
            }
        }
    }
}
