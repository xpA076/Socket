using FileManager.SocketLib;
using FileManager.SocketLib.Enums;
using FileManager.Static;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.Models
{
    /// <summary>
    /// 在大文件传输中 :
    /// 1.为子线程分配文件传输任务
    /// 2.向 server 端请求 fsid, 并保证线程安全
    /// </summary>
    public class FileTaskDispatcher
    {
        /// <summary>
        /// 在大文件传输过程中, 任一子线程触发无法处理的异常时(socket 认证 or server端文件读写异常)
        /// 将此 flag 置为 true, 其它线程会因此退出, 在 RunTransferSubThreads() 中将当前 task.Status 标为 Failed
        /// </summary>
        public bool IsCurrentTaskFailed { get; set; } = false;

        #region packet generator
        private readonly object PacketLock = new object();
        private readonly HashSet<int> TransferingPackets = new HashSet<int>();
        private readonly HashSet<int> FinishedPackets = new HashSet<int>();

        /// <summary>
        /// 已完成 packet 数量
        /// </summary>
        public int FinishedPacket 
        {
            get
            {
                return this.Task.FinishedPacket;
            }
            private set
            {
                this.Task.FinishedPacket = value;
            }
        }

        private int TotalPacket { get; set; }

        public FileTask Task { get; private set; }

        public FileTaskDispatcher(FileTask task)
        {
            IsCurrentTaskFailed = false;
            Task = task;
            FinishedPacket = task.FinishedPacket;
            TotalPacket = (int)(task.Length / HB32Encoding.DataSize) + (task.Length % HB32Encoding.DataSize > 0 ? 1 : 0);
        }

        public void Reset(FileTask task)
        {
            IsCurrentTaskFailed = false;
            Task = task;
            FinishedPacket = task.FinishedPacket;
            TotalPacket = (int)(task.Length / HB32Encoding.DataSize) + (task.Length % HB32Encoding.DataSize > 0 ? 1 : 0);
            lock (this.PacketLock)
            {
                TransferingPackets.Clear();
                FinishedPackets.Clear();
            }
            IsRequestingFsid = false;
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
        #endregion

        #region request fsid
        public int FileStreamId { get; private set; } = -1;

        public readonly object FsidLock = new object();

        /// <summary>
        /// Server 端重启后, 原有 fsid 不可用, 在某一 SubThread 中首先重新调用 GetFileStreamId()
        /// 将此 flag 置为 true
        /// 此时其余线程执行 SendHeader() 或 SendBytes() 应加锁, 保证在获取 fsid 后再发包
        /// </summary>
        public bool IsRequestingFsid { get; set; } = false;



        /// <summary>
        /// 根据 FileTask 向 Server 端请求 FileStreamId (16bit - 0~65535), 如有异常则值为 -1
        /// [* 未保证线程安全 *]
        /// </summary>
        /// <param name="task"></param>
        /// <returns></returns>
        public void RequestFileStreamId()
        {
            HB32Packet mask = (HB32Packet)((Task.Type == TransferTypeDeprecated.Upload ? 1 : 0) << 8);
            try
            {
                SocketClient client = SocketFactory.Instance.GenerateConnectedSocketClient(Task, 1);
                client.SendBytes(HB32Packet.DownloadFileStreamIdRequest | mask, Task.RemotePath);
                client.ReceiveBytesWithHeaderFlag(HB32Packet.DownloadAllowed ^ mask, out byte[] bytes);
                client.Close();
                string response = Encoding.UTF8.GetString(bytes);
                FileStreamId = int.Parse(response);
            }
            catch (Exception ex)
            {
                Logger.Log("Cannot get FileStreamID, Exception : " + ex.Message);
                System.Windows.Forms.MessageBox.Show(ex.Message);
                FileStreamId = - 1;
            }
        }




        #endregion
    }
}
