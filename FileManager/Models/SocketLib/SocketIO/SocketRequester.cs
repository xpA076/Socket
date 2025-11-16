using FileManager.Models.SocketLib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.Models.SocketLib.SocketIO
{
    public class SocketRequester : SocketEndPoint
    {

        public TCPAddress HostAddress { get; private set; }

        private IPEndPoint IPEndPoint;

        public SocketRequester(TCPAddress address)
        {
            HostAddress = address;
            IPEndPoint = new IPEndPoint(address.IP, address.Port);
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }


        private class ConnectTimeoutHandler
        {
            private readonly ManualResetEvent ConnectTimeoutObject = new ManualResetEvent(false);

            public bool IsSuccess { get; set; } = false;

            public Exception ConnectException { get; set; } = new Exception("null connect exception");


            public void Set()
            {
                ConnectTimeoutObject.Set();
            }

            public void Reset()
            {
                ConnectTimeoutObject.Reset();
            }

            public bool WaitOne(int millisecondsTimeout, bool exitContext)
            {
                return ConnectTimeoutObject.WaitOne(millisecondsTimeout, exitContext);
            }
        }

        private readonly ConnectTimeoutHandler cth = new ConnectTimeoutHandler();

        public void ConnectWithTimeout(int timeout)
        {
            cth.Reset();
            socket.BeginConnect(this.IPEndPoint, asyncResult =>
            {
                try
                {
                    cth.IsSuccess = false;
                    if (asyncResult.AsyncState is Socket s)
                    {
                        s.EndConnect(asyncResult);
                        cth.IsSuccess = true;
                    }
                }
                catch (Exception ex)
                {
                    cth.IsSuccess = false;
                    cth.ConnectException = ex;
                }
                finally
                {
                    cth.Set();
                }
            }, socket);
            if (cth.WaitOne(timeout, false))
            {
                if (cth.IsSuccess)
                {
                    return;
                }
                else
                {
                    throw cth.ConnectException;
                }

            }
            else
            {
                socket.Close();
                throw new TimeoutException("Connection timeout");
            }

        }
    }
}
