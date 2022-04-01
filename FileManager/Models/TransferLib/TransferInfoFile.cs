using FileManager.SocketLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.Models.TransferLib
{
    /// <summary>
    /// 传输任务的文件信息
    /// </summary>
    public class TransferInfoFile : TransferInfo
    {
        public int Priority { get; set; } = 0;

        public DateTime CreationTimeUtc { get; set; } = new DateTime(0);

        public DateTime LastWriteTimeUtc { get; set; } = new DateTime(0);


        public long FinishedPacket { get; set; } = 0;

        public TransferStatus Status { get; set; }

        private int _bytes_length = 0;

        public int BytesLength
        {
            get
            {
                if (_bytes_length == 0)
                {
                    byte[] bs_name = Encoding.UTF8.GetBytes(Name);
                    _bytes_length = 4 + bs_name.Length + 8 + 8 + 8 + 8 + 4;
                }
                return _bytes_length;
            }
            //set { _bytes_length = value; }
        }




        private const int bytes_init_capacity = 64;

        /// <summary>
        /// 利用 FileStream 写入文件
        /// </summary>
        /// <param name="fs"></param>
        /// <returns>当前节点字节总长度(4 byte 长度头标识中为后续长度)</returns>
        public int SaveToFile(FileStream fs)
        {
            BytesBuilder bb = new BytesBuilder(bytes_init_capacity);
            bb.Append(Priority);
            bb.Append(Name);
            bb.Append(Length);
            bb.Append(CreationTimeUtc);
            bb.Append(LastWriteTimeUtc);
            bb.Append(FinishedPacket);
            bb.Append((int)Status);
            byte[] bs = bb.GetBytes();
            fs.Write(BitConverter.GetBytes(bs.Length), 0, 4);
            fs.Write(bs, 0, bs.Length);
            _bytes_length = 4 + bs.Length;
            return _bytes_length;
        }


        public static TransferInfoFile ReadFromFile(FileStream fs)
        {
            byte[] b_len = new byte[4];
            fs.Read(b_len, 0, 4);
            int len = BitConverter.ToInt32(b_len, 0);
            byte[] bs = new byte[len];
            fs.Read(bs, 0, len);
            TransferInfoFile info = new TransferInfoFile();
            int idx = 0;
            info.Priority = BytesParser.GetInt(bs, ref idx);
            info.Name = BytesParser.GetString(bs, ref idx);
            info.Length = BytesParser.GetLong(bs, ref idx);
            info.CreationTimeUtc = BytesParser.GetDateTime(bs, ref idx);
            info.LastWriteTimeUtc = BytesParser.GetDateTime(bs, ref idx);
            info.FinishedPacket = BytesParser.GetLong(bs, ref idx);
            info.Status = (TransferStatus)BytesParser.GetInt(bs, ref idx);
            return info;
        }




    }
}
