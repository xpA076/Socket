﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketLib.SocketServer
{
    public class SocketServerFileStreamInfo
    {
        public FileStream FStream { get; set; }

        public string ServerPath { get; set; }

        public long Length { get; set; }

        public DateTime LastTime { get; set; }
    }
}
