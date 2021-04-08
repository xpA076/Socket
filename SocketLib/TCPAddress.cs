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

        public TCPAddress Copy()
        {
            return new TCPAddress
            {
                IP = this.IP,
                Port = this.Port
            };
        }
    }
}
