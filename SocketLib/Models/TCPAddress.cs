using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SocketLib
{
    public class TCPAddress
    {
        public IPAddress IP { get; set; } = null;
        public int Port { get; set; } = 0;


        public override string ToString()
        {
            return string.Format("{0}:{1}", IP, Port);
        }

        public static TCPAddress FromString(string s)
        {
            string[] ss = s.Split(':');
            return new TCPAddress
            {
                IP = IPAddress.Parse(ss[0]),
                Port = int.Parse(ss[1])
            };
        }


        public byte[] GetBytes()
        {
            byte[] ip_bytes = IP.GetAddressBytes();
            return new byte[6]
            {
                ip_bytes[0],
                ip_bytes[1],
                ip_bytes[2],
                ip_bytes[3],
                (byte)((Port >> 8) & 0xFF),
                (byte)(Port & 0xFF)
            };
        }


        public static TCPAddress FromBytes(byte[] bytes, int index = 0)
        {
            return new TCPAddress
            {
                IP = new IPAddress(new byte[4] {
                    bytes[index + 0], bytes[index + 1], bytes[index + 2], bytes[index + 3]
                }),
                Port = (((int)bytes[index + 4]) << 8) + (int)bytes[index + 5]
            };
        }

        public TCPAddress Copy()
        {
            return new TCPAddress
            {
                IP = new IPAddress(this.IP.GetAddressBytes()),
                Port = this.Port
            };
        }
    }
}
