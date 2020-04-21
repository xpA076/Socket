using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using SocketLib;

namespace SocketServerConsole
{
    class Program
    {
        static void Main(string[] args)
        {
         
            /*
            
            FileStream fs = new FileStream("4096-0.dat", FileMode.OpenOrCreate, FileAccess.Write);
            byte[] bytes = new byte[4096];

            for(int i = 0; i < 4 * 256; ++i)
            {
                for (int i0 = 0; i0 < 4096; ++i0)
                {
                    bytes[i0] = (byte)(i % 256);
                }
                fs.Write(bytes, 0, 4096);
            }
            fs.Close();
            */







            Config.LoadConfig();

            string name = Dns.GetHostName();
            IPAddress host = Dns.GetHostAddresses(Dns.GetHostName()).Where(ip => ip.AddressFamily == AddressFamily.InterNetwork).FirstOrDefault();

            Display.WriteLine(string.Format("Working as server ...\nIP address: {0}\nPort num: {1}", host.ToString(), Config.ServerPort.ToString()));

            

            SocketServer s;
            s = new SocketServer(host, Config.ServerPort);
            try
            {
                //s.InitializeServer();
                // 绑定端口，启动listen
                IPEndPoint ipe = new IPEndPoint(host, Config.ServerPort);
                s.server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                s.server.Bind(ipe);
                //s.server.SendTimeout = 3000;
                //s.server.ReceiveTimeout = 3000;
                s.server.Listen(20);
                Display.WriteLine("Server initiated.");
                // 从主线程创建监听线程
                //s.StartListen();
                Thread th_listen = new Thread(s.ServerListen);
                th_listen.IsBackground = true;
                th_listen.Start();
                Console.ReadLine();
                
            }
            catch (Exception ex)
            {
                s.Close();
                Display.WriteLine("Server window start listening error: " + ex.Message);
            }
            Console.ReadLine();
        }
    }
}
