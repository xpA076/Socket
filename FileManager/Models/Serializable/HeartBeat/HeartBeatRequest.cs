using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.Models.Serializable.HeartBeat
{
    public class HeartBeatRequest : ISocketSerializable
    {
        public static HeartBeatRequest Single
        {
            get
            {
                return new HeartBeatRequest();
            }
        }


        public static HeartBeatRequest FromBytes(byte[] bytes, int idx = 0)
        {
            HeartBeatRequest obj = new HeartBeatRequest();
            obj.BuildFromBytes(bytes, ref idx);
            return obj;
        }

        public void BuildFromBytes(byte[] bytes, ref int idx)
        {
            idx += 4;
        }

        public byte[] ToBytes()
        {
            return new byte[4] { 0x1, 0x2, 0x3, 0x4 };
        }
    }
}
