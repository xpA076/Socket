using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.Models.Serializable
{
    public interface ISocketSerializable
    {
        byte[] ToBytes();
        void BuildFromBytes(byte[] bytes);
    }
}
