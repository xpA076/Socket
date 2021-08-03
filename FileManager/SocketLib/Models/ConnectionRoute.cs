using FileManager.SocketLib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.SocketLib
{
    public class ConnectionRoute
    {
        //public RouteNode ServerAddress { get; set; } = new RouteNode();

        public RouteNode ServerAddress { get { return ProxyRoute.Last(); } }


        /// 代理规则 保证 ProxyRoute[0] 必为不含 Name 的路由节点
        public List<RouteNode> ProxyRoute { get; set; } = new List<RouteNode>();

        public RouteNode NextNode
        {
            get
            {
                return ProxyRoute[0];
            }
        }


        /// <summary>
        /// 下级代理是否为服务器 (byte传输是否不要发ProxyHeader包)
        /// </summary>
        public bool IsNextNodeProxy
        {
            get
            {
                return ProxyRoute.Count > 1;
            }
        }

        /// <summary>
        /// 从 ProxyRoute列表 指定位置开始获取其后的路由路径 bytes
        /// 2 bytes : 0x01 + [proxy list count]
        /// xx * proxy bytes : ProxyRouteNode
        /// </summary>
        /// <param name="node_start_index"></param>
        /// <returns></returns>
        public byte[] GetBytes(int node_start_index = 0)
        {
            if (ProxyRoute.Count <= node_start_index)
            {
                throw new ArgumentException("ProxyRoute index error");
            }
            List<byte[]> node_bytes = new List<byte[]>();
            int len = 2;
            for (int i = node_start_index; i < ProxyRoute.Count; ++i)
            {
                byte[] bs = ProxyRoute[i].GetBytes();
                node_bytes.Add(bs);
                len += bs.Length;
            }
            byte[] bytes = new byte[len];
            bytes[0] = 1;
            bytes[1] = (byte)(ProxyRoute.Count - node_start_index);
            int pt = 2;
            foreach(byte[] bs in node_bytes)
            {
                Array.Copy(bs, 0, bytes, pt, bs.Length);
                pt += bs.Length;
            }
            return bytes;
        }


        public static ConnectionRoute FromBytes(byte[] bytes, int index = 0)
        {
            return FromBytes(bytes, ref index);
        }


        /// <summary>
        /// 从 bytes 的 index 位置起还原 ConnectionRoute
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static ConnectionRoute FromBytes(byte[] bytes, ref int index)
        {
            if (bytes[index] == 1)
            {
                byte count = bytes[index + 1];
                ConnectionRoute c = new ConnectionRoute();
                index += 2;
                for (int i = 0; i < count; ++i)
                {
                    c.ProxyRoute.Add(RouteNode.FromBytes(bytes, ref index));
                }
                return c;
            }
            else
            {
                return null;
            }
        }


        /// <summary>
        /// 解析UI字符串至 ConnectionRoute 对象
        /// 字符串格式:
        ///   server : IPx or IPx-name_x
        ///   proxy  : [empty] or IP1-name_1;IP2;IP3-name_3;......
        /// 解析规则:
        ///   IP_n-name_n 解析为两个 RouteNode, 按顺序分别为 IPn 和 IPn-name_n
        ///   IP_n 解析为一个 RouteNode, 其 Name 为 ""
        ///   server 若为挂在反向代理的server, 则代理列表最后一级为反向代理服务器IP
        /// 解析规则保证 ProxyRoute[0].Name == "" (若存在), 即一定不是反向代理
        /// </summary>
        /// <param name="server_string"></param>
        /// <param name="proxy_string"></param>
        /// <returns></returns>
        public static ConnectionRoute FromString(string server_string, string proxy_string, int default_server_port = 12138, int default_proxy_port = 12139)
        {
            ConnectionRoute cr = new ConnectionRoute();
            if (!string.IsNullOrEmpty(proxy_string))
            {
                string[] proxies = proxy_string.Split(';');
                foreach(string proxy0 in proxies)
                {
                    if (proxy0.Contains("-"))
                    {
                        string[] proxy0_split = proxy0.Split('-');
                        if (!proxy0_split[0].Contains(':')) { proxy0_split[0] += ":" + default_proxy_port.ToString(); }
                        cr.ProxyRoute.Add(new RouteNode(proxy0_split[0]));
                        cr.ProxyRoute.Add(new RouteNode(proxy0_split[0], proxy0_split[1]));
                    }
                    else
                    {
                        string proxy_str = proxy0;
                        if (!proxy_str.Contains(':')) { proxy_str += ":" + default_proxy_port.ToString(); }
                        cr.ProxyRoute.Add(new RouteNode(proxy_str));
                    }
                }
            }
            if (server_string.Contains("-"))
            {
                string[] strs = server_string.Split('-');
                if (!strs[0].Contains(':')) { strs[0] += ":" + default_server_port.ToString(); }
                cr.ProxyRoute.Add(new RouteNode(strs[0]));
                cr.ProxyRoute.Add(new RouteNode(strs[0], strs[1]));
            }
            else
            {
                if (!server_string.Contains(':')) { server_string += ":" + default_server_port.ToString(); }
                cr.ProxyRoute.Add(new RouteNode(server_string));
            }
            return cr;
        }
        

        public ConnectionRoute Copy()
        {
            ConnectionRoute route = new ConnectionRoute
            {
                ProxyRoute = new List<RouteNode>()
            };
            foreach (RouteNode node in this.ProxyRoute)
            {
                route.ProxyRoute.Add(node.Copy());
            }
            return route;
        }


    }
}
