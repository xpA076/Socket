using FileManager.SocketLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.Models
{
    public enum TransferFileInfoStatus : int
    {
        Waiting, 
        Success,
        //Denied,
        Failed,
        Transfering,

    }


    public class TransferFileInfo : TransferInfo
    {

        public long Length { get; set; }

        public DateTime CreationTimeUtc { get; set; } = new DateTime(0);

        public DateTime LastWriteTimeUtc { get; set; } = new DateTime(0);


        public long FinishedPacket { get; set; } = 0;

        public TransferFileInfoStatus Status { get; set; }

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
        }




        private const int bytes_init_capacity = 64;

        public void SaveToFile(FileStream fs)
        {
            BytesBuilder bb = new BytesBuilder(bytes_init_capacity);
            bb.Append(Name);
            bb.Append(Length);
            bb.Append(CreationTimeUtc);
            bb.Append(LastWriteTimeUtc);
            bb.Append(FinishedPacket);
            bb.Append((int)Status);
            byte[] bs = bb.GetBytes();
            fs.Write(BitConverter.GetBytes(bs.Length), 0, 4);
            fs.Write(bs, 0, bs.Length);
        }


        public static TransferFileInfo ReadFromFile(FileStream fs)
        {
            byte[] b_len = new byte[4];
            fs.Read(b_len, 0, 4);
            int len = BitConverter.ToInt32(b_len, 0);
            byte[] bs = new byte[len];
            fs.Read(bs, 0, len);
            TransferFileInfo info = new TransferFileInfo();
            int idx = 0;
            info.Name = BytesParser.GetString(bs, ref idx);
            info.Length = BytesParser.GetLong(bs, ref idx);
            info.CreationTimeUtc = BytesParser.GetDateTime(bs, ref idx);
            info.LastWriteTimeUtc = BytesParser.GetDateTime(bs, ref idx);
            info.FinishedPacket = BytesParser.GetLong(bs, ref idx);
            info.Status = (TransferFileInfoStatus)BytesParser.GetInt(bs, ref idx);
            return info;
        }




    }
}
