using FileManager.Utils.Bytes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.Models.SocketLib.HbProtocol
{
    public class HB16Header
    {
        public const int Length = 16;

        public byte Proxy1 { get; set; }

        public byte Proxy2 { get; set; }

        public byte Header1 { get; set; }

        public byte Header2 { get; set; }

        public UInt32 U32Header
        {
            get
            {
                return (((UInt32)this.Proxy1) << 24) +
                    (((UInt32)this.Proxy2) << 16) +
                    (((UInt32)this.Header1) << 8) +
                    (((UInt32)this.Header2) << 0);
            }
            set
            {
                this.Proxy1 = (byte)((value >> 24) & 0xFF);
                this.Proxy2 = (byte)((value >> 16) & 0xFF);
                this.Header1 = (byte)((value >> 8) & 0xFF);
                this.Header2 = (byte)((value >> 0) & 0xFF);
            }
        }

        public int ValidByteLength { get; set; }

        public int RemainByteLength { get; set; }

        public int TotalByteLength { get; set; }

        public HB16Header()
        {

        }

        public HB16Header(byte[] bs)
        {
            this.Proxy1 = bs[0];
            this.Proxy2 = bs[1];
            this.Header1 = bs[2];
            this.Header2 = bs[3];
            int idx = 4;
            this.ValidByteLength = BytesParser.GetInt(bs, ref idx);
            this.RemainByteLength = BytesParser.GetInt(bs, ref idx);
            this.TotalByteLength = BytesParser.GetInt(bs, ref idx);
        }

        public void BuildByU32(UInt32 u32h)
        {
            this.Proxy1 = (byte)((u32h >> 24) & 0xFF);
            this.Proxy2 = (byte)((u32h >> 16) & 0xFF);
            this.Header1 = (byte)((u32h >> 8) & 0xFF);
            this.Header2 = (byte)(u32h & 0xFF);
        }


        public byte[] GetBytes()
        {
            byte[] bs = new byte[16];
            bs[0] = Proxy1;
            bs[1] = Proxy2;
            bs[2] = Header1;
            bs[3] = Header2;
            int idx = 4;
            BytesConverter.WriteInt(bs, ValidByteLength, ref idx);
            BytesConverter.WriteInt(bs, RemainByteLength, ref idx);
            BytesConverter.WriteInt(bs, TotalByteLength, ref idx);
            return bs;
        }

    }
}
