using FileManager.SocketLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.Models.Serializable
{
    /// <summary>
    /// 用于在Socket建立时
    /// 利用 KeyBytes 创建 session 或利用 SessionBytes 加入现有 session
    /// </summary>
    public class SessionRequest : ISocketSerializable
    {
        public enum BytesType : int
        {
            KeyBytes,
            SessionBytes
        }

        public BytesType Type { get; set; }

        public byte[] Bytes { get; set; } = new byte[0];

        public static SessionRequest FromBytes(byte[] bytes, int idx = 0)
        {
            SessionRequest obj = new SessionRequest();
            obj.BuildFromBytes(bytes, ref idx);
            return obj;
        }

        public byte[] ToBytes()
        {
            BytesBuilder bb = new BytesBuilder();
            bb.Append((int)Type);
            bb.Append(Bytes);
            return bb.GetBytes();
        }

        public void BuildFromBytes(byte[] bytes, ref int idx)
        {
            this.Type = (BytesType)BytesParser.GetInt(bytes, ref idx);
            this.Bytes = BytesParser.GetBytes(bytes, ref idx);
        }
    }
}
