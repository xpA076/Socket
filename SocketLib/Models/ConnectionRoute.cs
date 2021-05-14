using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketLib
{
    public class ConnectionRoute
    {
        public TCPAddress ServerAddress { get; set; } = new TCPAddress();

        public List<TCPAddress> ProxyRoute { get; set; } = new List<TCPAddress>();


        /// <summary>
        /// 2 bytes : 01 + [proxy count]
        /// 6 bytes : ServerAddress
        /// 6*proxy bytes : ProxyRoute
        /// </summary>
        /// <returns></returns>
        public byte[] GetBytes()
        {
            byte[] bytes = new byte[2 + 6 + 6 * ProxyRoute.Count];
            bytes[0] = 1;
            bytes[1] = (byte)ProxyRoute.Count;
            Array.Copy(ServerAddress.GetBytes(), 0, bytes, 2, 6);
            int pt = 8;
            for (int i = 0; i < ProxyRoute.Count; ++i)
            {
                Array.Copy(ProxyRoute[i].GetBytes(), 0, bytes, pt, 6);
                pt += 6;
            }
            return bytes;
        }


        public byte[] GetBytesExceptFirstProxy()
        {
            if (ProxyRoute.Count == 0)
            {
                throw new ArgumentNullException("ProxyRoute");
            }
            byte[] bytes = new byte[2 + 6 + 6 * (ProxyRoute.Count - 1)];
            bytes[0] = 1;
            bytes[1] = (byte)ProxyRoute.Count;
            Array.Copy(ServerAddress.GetBytes(), 0, bytes, 2, 6);
            int pt = 8;
            for (int i = 1; i < ProxyRoute.Count; ++i)
            {
                Array.Copy(ProxyRoute[i].GetBytes(), 0, bytes, pt, 6);
                pt += 6;
            }
            return bytes;
        }


        public static ConnectionRoute FromBytes(byte[] bytes, int index = 0)
        {
            int pt = index;
            if (bytes[pt] == 1)
            {
                ConnectionRoute c = new ConnectionRoute();
                c.ServerAddress = TCPAddress.FromBytes(bytes, pt + 2);
                pt += 8;
                for (int i = 0; i < bytes[1]; ++i)
                {
                    c.ProxyRoute.Add(TCPAddress.FromBytes(bytes, pt));
                    pt += 6;
                }
                return c;
            }
            return null;
        }

        public ConnectionRoute Copy()
        {
            ConnectionRoute route = new ConnectionRoute
            {
                ServerAddress = this.ServerAddress,
                ProxyRoute = new List<TCPAddress>()
            };
            foreach (TCPAddress tcp_addr in this.ProxyRoute)
            {
                route.ProxyRoute.Add(tcp_addr.Copy());
            }
            return route;
        }

    }
}
