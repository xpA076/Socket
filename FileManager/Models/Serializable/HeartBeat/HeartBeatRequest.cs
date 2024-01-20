using FileManager.Models.SocketLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.Models.Serializable.HeartBeat
{
    public class HeartBeatRequest : ISocketSerializable
    {
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
            return new byte[4];
        }
    }
}
