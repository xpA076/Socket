using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.SocketLib
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
            Append(new byte[1] { value ? (byte)1 : (byte)0 });
        }

        public void Append(int value)
        {
            Append(BitConverter.GetBytes(value));
        }

        public void Append(long value)
        {
            Append(BitConverter.GetBytes(value));
        }

        public void Append(string value)
        {
            Append(Encoding.UTF8.GetByteCount(value));
            Append(Encoding.UTF8.GetBytes(value));
        }

        public void Append(DateTime value)
        {
            Append(BitConverter.GetBytes(value.Ticks));
        }


        public void Append(byte[] bytes)
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

        /// <summary>
        /// 向 byte 流写入前额外先写入 4byte-int 的 bytes 长度
        /// </summary>
        /// <param name="bytes"></param>
        public void AppendWithLength(byte[] bytes)
        {
            Append(bytes.Length);
            Append(bytes);
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
