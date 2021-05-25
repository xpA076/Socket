using FileManager.SocketLib.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.SocketLib
{
    public class SocketEndPoint : SocketIO
    {
        protected Socket client = null;

        public bool IsWithProxy { get; set; } = false;


        #region Send / Receive

        public void SendRawbytes(byte[] bytes)
        {
            client.Send(bytes);
        }


        public void SendHeader(HB32Header header)
        {
            if (IsWithProxy)
            {
                SendHeader(client, header, new byte[2] { SocketProxy.ProxyHeaderByte, (byte)ProxyHeader.SendHeader });
            }
            else
            {
                SendHeader(client, header);
            }
        }


        public void SendHeader(SocketPacketFlag flag, int i1 = 0, int i2 = 0, int i3 = 0)
        {
            SendHeader(new HB32Header
            {
                Flag = flag,
                I1 = i1,
                I2 = i2,
                I3 = i3
            });
        }


        public void SendBytes(HB32Header header, byte[] bytes)
        {
            if (IsWithProxy)
            {
                SendBytes(client, header, bytes, new byte[2] { SocketProxy.ProxyHeaderByte, (byte)ProxyHeader.SendBytes });
            }
            else
            {
                SendBytes(client, header, bytes);
            }

        }


        public void SendBytes(SocketPacketFlag flag, byte[] bytes, int i1 = 0, int i2 = 0, int i3 = 0)
        {
            SendBytes(new HB32Header
            {
                Flag = flag,
                I1 = i1,
                I2 = i2,
                I3 = i3
            }, bytes);
        }

        public void SendBytes(SocketPacketFlag flag, string str, int i1 = 0, int i2 = 0, int i3 = 0)
        {
            SendBytes(flag, Encoding.UTF8.GetBytes(str), i1, i2, i3);
        }


        public void ReceiveBytes(out HB32Header header, out byte[] bytes)
        {
            if (IsWithProxy)
            {
                client.Send(new byte[2] { 0xA3, (byte)ProxyHeader.ReceiveBytes });
            }
            ReceiveBytes(client, out header, out bytes);
        }


        public void ReceiveBytesWithHeaderFlag(SocketPacketFlag flag, out HB32Header header, out byte[] bytes)
        {
            ReceiveBytes(out header, out bytes);
            if (header.Flag != flag)
            {
                string err_msg = "";
                try
                {
                    err_msg = Encoding.UTF8.GetString(bytes);
                }
                catch (Exception) {; }
                throw new ArgumentException(string.Format("[Received not valid header: {0}, required : {1}] -- {2}", header.Flag.ToString(), flag.ToString(), err_msg));
            }
        }


        public void ReceiveBytesWithHeaderFlag(SocketPacketFlag flag, out HB32Header header)
        {
            ReceiveBytesWithHeaderFlag(flag, out header, out _);
        }


        public void ReceiveBytesWithHeaderFlag(SocketPacketFlag flag, out byte[] bytes)
        {
            ReceiveBytesWithHeaderFlag(flag, out _, out bytes);
        }


        #endregion




        public void CloseSocket()
        {
            try
            {
                client.Close();
            }
            catch { }
        }
    }
}
