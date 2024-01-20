using FileManager.Models.SocketLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.Utils.Bytes
{
    public class BytesBuilder
    {
        private byte[] _bytes;

        private int _length = 0;

        public int Length
        {
            get 
            {
                return _length;
            }
        }

        private int _capacity = 32;


        public BytesBuilder()
        {
            _bytes = new byte[_capacity];
        }


        public BytesBuilder(int capacity)
        {
            _capacity = capacity;
            _bytes = new byte[_capacity];
        }


        public byte[] GetBytes()
        {
            return _bytes.Take(_length).ToArray();
        }

        public void Append(bool value)
        {
            AppendBytes(new byte[1] { value ? (byte)1 : (byte)0 });
        }

        public void Append(int value)
        {
            AppendBytes(BitConverter.GetBytes(value));
        }

        public void Append(long value)
        {
            AppendBytes(BitConverter.GetBytes(value));
        }

        public void Append(string value)
        {
            Append(Encoding.UTF8.GetByteCount(value));
            AppendBytes(Encoding.UTF8.GetBytes(value));
        }

        public void Append(DateTime value)
        {
            AppendBytes(BitConverter.GetBytes(value.Ticks));
        }

        /// <summary>
        /// 每个 byte 保存一个 bool 信息
        /// </summary>
        /// <param name="value"></param>
        public void AppendListBool(List<bool> value)
        {
            byte[] bytes = new byte[value.Count];
            for (int i = 0; i < value.Count; ++i)
            {
                bytes[i] = (byte)(value[i] ? 1 : 0);
            }
            Append(bytes.Length);
            AppendBytes(bytes);
        }

        /// <summary>
        /// 向 byte 流写入前额外先写入 4byte-int 的 bytes 长度
        /// </summary>
        /// <param name="bytes"></param>
        public void Append(byte[] bytes)
        {
            Append(bytes.Length);
            AppendBytes(bytes);
        }


        public void AppendList<T>(List<T> value) where T : ISocketSerializable
        {
            if (value == null)
            {
                Append((int)0);
            }
            else
            {
                Append(value.Count);
                for (int i = 0; i < value.Count; ++i)
                {
                    AppendBytes(value[i].ToBytes());
                }
            }
        }

        public void Concatenate(byte[] bytes)
        {
            this.AppendBytes(bytes);
        }


        /// <summary>
        /// 供所有 public 的 Append() 方法调用
        /// 直接进行 BytesBuilder 字节流的追加写入
        /// </summary>
        /// <param name="bytes"></param>
        private void AppendBytes(byte[] bytes)
        {
            if (bytes.Length == 0)
            {
                return;
            }
            if (_length + bytes.Length > _capacity)
            {
                Expand(_length + bytes.Length);
            }
            Array.Copy(bytes, 0, _bytes, _length, bytes.Length);
            _length += bytes.Length;
        }





        private void Expand(int min_required_capacity)
        {
            while(_capacity < min_required_capacity)
            {
                _capacity = NextCapacity(_capacity);
            }
            byte[] new_bytes = new byte[_capacity];
            Array.Copy(_bytes, new_bytes, _length);
            _bytes = new_bytes;
        }

        private int NextCapacity(int c)
        {
            return c * 2;
        }

    }
}
