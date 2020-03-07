using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketServerConsole
{
    public static class Display
    {
        public static void WriteLine(string str)
        {
            Console.WriteLine(str);
        }
        public static void Write(string str)
        {
            Console.Write(str);
        }
        public static void TimeWriteLine(string str)
        {
            Console.WriteLine(DateTime.Now.ToString() + "  " + str);
        }


    }
}
