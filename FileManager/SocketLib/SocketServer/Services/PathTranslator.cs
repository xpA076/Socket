using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.SocketLib.SocketServer.Services
{
    public class PathTranslator
    {
        public string ToTruePath(string path, SocketSession session)
        {
            return path;
        }

        public string ToViewPath(string path, SocketSession session)
        {
            return path;
        }

    }
}
