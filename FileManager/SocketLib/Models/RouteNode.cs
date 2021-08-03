using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.SocketLib.Models
{
    public class RouteNode
    {
        public TCPAddress Address { get; set; }

        public string Name { get; set; } = "";

        public RouteNode()
        {

        }


        public RouteNode(TCPAddress address)
        {
            Address = address.Copy();
        }

        public RouteNode(string address_string)
        {
            Address = TCPAddress.FromString(address_string);
        }


        public RouteNode(TCPAddress address, string name)
        {
            Address = address.Copy();
            Name = name;
        }

        /// <summary>
        /// 初始化 RouteNode
        /// </summary>
        /// <param name="address_string">形如xxx.xxx.xxx.xxx:12345</param>
        /// <param name="name">反向代理服务名称字符串</param>
        public RouteNode(string address_string, string name)
        {
            Address = TCPAddress.FromString(address_string);
            Name = name;
        }


        public RouteNode Copy()
        {
            return new RouteNode
            {
                Address = this.Address,
                Name = this.Name
            };
        }

        public byte[] GetBytes()
        {
            byte[] tcp_bytes = Address.GetBytes();
            return BytesConverter.WriteString(tcp_bytes, Name, tcp_bytes.Length);
        }


        public static RouteNode FromBytes(byte[] bytes, int index)
        {
            return FromBytes(bytes, ref index);
        }

        public static RouteNode FromBytes(byte[] bytes, ref int index)
        {
            RouteNode rn = new RouteNode();
            rn.Address = TCPAddress.FromBytes(bytes, ref index);
            rn.Name = BytesConverter.ParseString(bytes, ref index);
            return rn;
        }


        public override string ToString()
        {
            if (string.IsNullOrEmpty(Name)) 
            {
                return Address.ToString();
            }
            else
            {
                return Address.ToString() + "-" + Name;
            }
            
        }

    }
}
