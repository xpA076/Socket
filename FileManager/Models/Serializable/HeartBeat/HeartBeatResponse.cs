using FileManager.Models.SocketLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.Models.Serializable.HeartBeat
{
    public class HeartBeatResponse : ISocketSerializable
    {
        public static HeartBeatResponse FromBytes(byte[] bytes, int idx = 0)
        {
            HeartBeatResponse obj = new HeartBeatResponse();
            obj.BuildFromBytes(bytes, ref idx);
            return obj;
        }

        public void BuildFromBytes(byte[] bytes, ref int idx)
        {
            idx += 4;
        }

        public byte[] ToBytes()
        {
            return new byte[4];
        }
    }
}
